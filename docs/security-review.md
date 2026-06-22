# OYPA CRM — Güvenlik Denetim Raporu

- **Denetim türü:** Salt-okunur (READ-ONLY). Hiçbir kod dosyası değiştirilmedi.
- **Tarih:** 2026-06-08
- **Denetleyen:** Security Agent
- **Kapsam:** Backend (Api / Application / Infrastructure / Domain) auth & güvenlik yüzeyi + Frontend token saklama.

---

## Özet Tablo

| # | Bulgu | Önem |
|---|-------|------|
| 1 | HTTPS yönlendirme + HSTS yok; güvenlik header'ları (X-Content-Type-Options, X-Frame-Options, CSP, Referrer-Policy) eksik | **High** |
| 2 | JWT secret uzunluğu/gücü başlangıçta doğrulanmıyor (zayıf/kısa anahtar HS256'yı çökertir) | **High** |
| 3 | Seed admin parolası kaynak kodda sabit fallback (`Admin!23456`) | **High** |
| 4 | Access token frontend'de `localStorage`'da — XSS ile token çalınabilir | **High** |
| 5 | Login rate-limit partition'ı yalnızca IP bazlı (kullanıcı adı/e-posta yok) — kullanıcı-hedefli brute force ve NAT arkası yanlış pozitif | **Medium** |
| 6 | `AllowedHosts: "*"` — Host header kısıtı yok | **Medium** |
| 7 | `register` endpoint'i parola karmaşıklığını yalnızca uzunlukla doğruluyor (Identity politikası ile validator tutarsız) | **Low** |
| 8 | Reuse-detection yalnızca DB'de hâlâ duran iptal edilmiş token'a dayanıyor; eski token temizleme/expiry job yok | **Low** |
| 9 | CORS `AllowAnyHeader().AllowAnyMethod()` — kabul edilebilir ama daraltılabilir | **Low** |
| 10 | ClockSkew 30 sn — bilinçli ve iyi; bilgi amaçlı not | **Info** |

> İyi haber: Birçok kritik kontrol zaten doğru yapılmış (aşağıda "Doğru Yapılanlar" bölümü). Critical seviyesinde açık bulunmadı.

---

## Bulgular (Detaylı)

### 1. HTTPS yönlendirme + HSTS + güvenlik header'ları eksik — **High**

**Kanıt:**
- `backend/src/Api/Program.cs:50-64` — pipeline'da `UseHttpsRedirection`, `UseHsts` ve hiçbir güvenlik header middleware'i yok.
- Repo genelinde arama: `UseHttpsRedirection | UseHsts | X-Content-Type-Options | X-Frame-Options | Content-Security-Policy` → **0 eşleşme**.

**Risk:** Düz HTTP üzerinden token/parola sızıntısı (MITM), tarayıcı tarafı MIME-sniffing, clickjacking, downgrade saldırıları. Production'da TLS sonlandırma reverse-proxy'de yapılsa bile HSTS ve içerik header'ları uygulamada da olmalı.

**Önerilen düzeltme (taslak):**
```csharp
// Program.cs — app.Build() sonrası, UseCors'tan önce
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();

app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "no-referrer";
    h["Content-Security-Policy"] = "default-src 'self'; frame-ancestors 'none'";
    await next();
});
```

---

### 2. JWT secret uzunluğu doğrulanmıyor — **High**

**Kanıt:**
- `backend/src/Api/Extensions/AuthenticationExtensions.cs:13-15` — secret yokluğunda hata fırlatılıyor (iyi), ancak **uzunluk/güç kontrolü yok**.
- `backend/src/Infrastructure/Security/JwtTokenService.cs:30-31` — `SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret))` + `HmacSha256`.
- `.env.example:13` "EN AZ 32 KARAKTER" diyor ama bu yalnızca dokümantasyon; runtime'da zorlanmıyor.

**Risk:** HS256 için anahtar < 256 bit (32 byte) ise imza zayıflar; operatör 8 karakterlik bir secret koyarsa uygulama sessizce çalışır ve token'lar kaba kuvvetle taklit edilebilir hale gelir.

**Önerilen düzeltme (taslak):**
```csharp
var secret = configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret yapılandırılmamış...");
if (Encoding.UTF8.GetByteCount(secret) < 32)
    throw new InvalidOperationException("Jwt:Secret en az 32 byte (256 bit) olmalıdır.");
```

---

### 3. Seed admin parolası kaynak kodda sabit fallback — **High**

**Kanıt:**
- `backend/src/Infrastructure/Persistence/DbSeeder.cs:34-35`:
  ```csharp
  var adminEmail = config["Seed:AdminEmail"] ?? "admin@oypa.com.tr";
  var adminPassword = config["Seed:AdminPassword"] ?? "Admin!23456";
  ```
- Seed `Program.cs:67-77` ile Testing dışı tüm ortamlarda (Development + Production) çalışır.

**Risk:** Production'da `Seed:AdminPassword` env verilmezse, herkesçe bilinen sabit parolayla bir Admin hesabı oluşur — tam yetkili hesap ele geçirilir.

**Önerilen düzeltme:** Fallback parolasını kaldır; yoksa (özellikle non-Development ortamda) seed'i atla veya hata ver:
```csharp
var adminPassword = config["Seed:AdminPassword"]
    ?? throw new InvalidOperationException("Seed:AdminPassword zorunludur.");
```
Alternatif: seed'i yalnızca Development'a sınırla.

---

### 4. Frontend access token `localStorage`'da — XSS riski — **High** (yalnızca rapor; kod değişikliği yapılmadı)

**Kanıt:**
- `frontend/src/shared/api/tokenStorage.ts:4-17` — hem access hem refresh token `localStorage`'da.
- `frontend/src/shared/api/httpClient.ts:43-49` — her istekte `localStorage`'dan okunup `Authorization` header'ına yazılıyor.

**Risk:** Herhangi bir XSS açığı (3rd-party script, bağımlılık zafiyeti) `localStorage`'daki token'lara JS ile erişip oturumu çalabilir; HttpOnly koruması yoktur.

**Önerilen yaklaşım (rapor önerisi):**
- Refresh token'ı `HttpOnly; Secure; SameSite=Strict` cookie ile sunucu tarafında tut; access token'ı yalnızca bellekte (in-memory, React state/closure) sakla, sayfa yenilemede sessiz refresh ile yeniden al.
- Bu, backend'de `/auth/refresh`'in cookie okuyacak şekilde değişmesini gerektirir (ayrı bir geliştirme görevi). Bu raporda kod değişikliği yapılmamıştır.

---

### 5. Login rate-limit partition'ı yalnızca IP bazlı — **Medium**

**Kanıt:**
- `backend/src/Api/Extensions/RateLimitingExtensions.cs:22-24` — `AuthLogin` partition'ı `ClientIp(ctx)` (sadece IP).
- `:57-58` — `ClientIp` salt `RemoteIpAddress`.

**Risk:**
1. NAT/kurumsal proxy arkasındaki birçok meşru kullanıcı tek IP'den gelir → yanlış pozitif kilitlenme.
2. Dağıtık IP'lerden tek bir kullanıcı hesabına yönelik (credential stuffing) brute force IP-limitini deler.

**Rol kuralı** (`security.md`) `auth-login` için **IP + kullanıcı** kombinasyonu öngörüyor.

**Önerilen düzeltme (taslak):** Login isteğinde gövdedeki e-postayı partition anahtarına dahil et (gövde okumak için endpoint'te ek limit veya middleware gerekir). Pratik yaklaşım: hem IP hem e-posta için ayrı limitler. Basit varyant:
```csharp
options.AddPolicy(AuthLogin, ctx =>
{
    var ip = ClientIp(ctx);
    var email = ctx.Request.Headers["X-Login-Hint"].ToString(); // veya route/query
    var key = string.IsNullOrEmpty(email) ? ip : $"{ip}:{email}";
    return RateLimitPartition.GetFixedWindowLimiter(key,
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) });
});
```
> Not: Gövdeyi rate-limiter içinde okumak akış (stream) sorunları doğurabilir; alternatif olarak başarısız login sayacını `IdentityService`/lockout ile (ASP.NET Identity `Lockout`) tutmak daha sağlamdır. `UserManager.AccessFailedCount` + `SupportsUserLockout` etkinleştirilmesi önerilir.

`auth-refresh` (`:27-29`) da IP-bazlı; refresh token zaten benzersiz olduğundan bu kabul edilebilir.

---

### 6. `AllowedHosts: "*"` — **Medium**

**Kanıt:** `backend/src/Api/appsettings.json:20` → `"AllowedHosts": "*"`.

**Risk:** Host header injection / cache poisoning yüzeyi. Production'da beklenen domain(ler)le sınırlanmalı.

**Önerilen düzeltme:** `appsettings.Production.json` veya env ile:
```json
"AllowedHosts": "crm.oypa.com.tr;api.oypa.com.tr"
```

---

### 7. Register parola politikası validator'da eksik — **Low**

**Kanıt:**
- `backend/src/Application/Features/Auth/Validators/RegisterUserRequestValidator.cs:11` — yalnızca `MinimumLength(8).MaximumLength(128)`.
- Identity politikası `backend/src/Infrastructure/DependencyInjection.cs:39-43` — digit + uppercase + non-alphanumeric zorunlu.

**Risk:** Düşük; Identity katmanı yine de reddeder (defansif). Ancak hata mesajları farklı katmandan döner, kullanıcı deneyimi/tutarlılık zayıf. `Role` alanı serbest string (`:13`) — geçersiz rol sessizce eklenmez (`IdentityService:45-48` yalnızca var olan rolü ekler) ama beyaz liste yok.

**Önerilen düzeltme:** Validator'a parola karmaşıklık kuralları ve `Role` için `Must(r => r is "Admin" or "Sales")` beyaz listesi ekle.

---

### 8. Refresh token temizleme/expiry job yok — **Low**

**Kanıt:**
- `backend/src/Application/Features/Auth/AuthService.cs:38-43` — reuse-detection iptal edilmiş token'ın DB'de **bulunmasına** dayanır (doğru tasarım).
- `RefreshTokenConfiguration.cs` / repository'de süresi dolmuş token'ları silen bir mekanizma yok.

**Risk:** Düşük (güvenlik açığından çok hijyen). Tablo zamanla şişer; reuse-detection için süresi dolmuş kayıtların tutulması gerekli olsa da çok eski kayıtlar için bir retention politikası iyi olur.

**Önerilen düzeltme:** Periyodik bir background service ile `ExpiresAtUtc < now - 30gün` kayıtları temizle. Cascade FK (`RefreshTokenConfiguration.cs:26-29`) zaten kullanıcı silinince token'ları temizliyor (iyi).

---

### 9. CORS — header/method kısıtı geniş — **Low**

**Kanıt:** `backend/src/Api/Program.cs:44-48` — `WithOrigins(origins)` (iyi, whitelist `appsettings.json:11-13`'ten) ama `AllowAnyHeader().AllowAnyMethod()`.

**Risk:** Düşük; origin kısıtı doğru olduğu için ana koruma yerinde. İstenirse method/header daraltılabilir. `AllowCredentials()` kullanılmıyor (cookie tabanlı auth'a geçilirse dikkat).

---

### 10. ClockSkew = 30 sn — **Info (doğru yapılmış)**

`AuthenticationExtensions.cs:34` — varsayılan 5 dakika yerine 30 sn'ye düşürülmüş. Kısa ömürlü token (15 dk) ile uyumlu, iyi bir sıkılaştırma.

---

## Doğru Yapılanlar (Pozitif Bulgular)

- **JWT secret appsettings'te DEĞİL:** `appsettings.json` / `appsettings.Development.json`'da `Jwt:Secret` yok; yalnızca env/user-secrets'tan okunuyor (`AuthenticationExtensions.cs:13`). Yokluğunda uygulama hata fırlatıyor. ✅
- **JWT doğrulama tam:** Issuer, Audience, Lifetime, IssuerSigningKey doğrulamaları açık (`AuthenticationExtensions.cs:27-33`). `MapInboundClaims=false` ile claim karışıklığı engellenmiş. ✅
- **Refresh token DB'de hash'li (SHA-256):** Düz token saklanmıyor (`JwtTokenService.cs:46-47`, `RefreshToken.cs:6-9`). Token 64 byte CSPRNG (`RandomNumberGenerator`) ile üretiliyor. ✅
- **Rotasyon + reuse-detection:** `AuthService.RefreshAsync` eski token'ı iptal edip yenisini bağlıyor; iptal edilmiş token tekrar kullanılırsa tüm aktif oturumlar sonlandırılıyor (`AuthService.cs:38-57`). ✅
- **TokenHash unique index + cascade FK:** `RefreshTokenConfiguration.cs:23, 26-29`. ✅
- **Authorization kapsaması:** Tüm iş controller'ları sınıf düzeyinde `[Authorize]`; hassas işlemler `[Authorize(AdminOnly)]` (`AuthController.cs:50`, `SalesRepsController.cs:23`, `TargetsController.cs:24`). Yalnızca `login`/`refresh` `[AllowAnonymous]`. Eksik `[Authorize]` tespit edilmedi. ✅
- **Hata yönetimi sızdırmıyor:** `GlobalExceptionMiddleware.cs:32` — beklenmeyen hatalar client'a "Beklenmeyen bir hata oluştu." döner; stack trace yalnızca server log'una yazılır (`:35-36`). ✅
- **Rate limiting politikaları mevcut:** login/refresh/arama/admin-sensitive ayrı ayrı tanımlı, OnRejected zarflı 429 döner. ✅
- **Input validation:** FluentValidation + global `ValidationFilter` + model-state zarfı (`Program.cs:18, 23-33`). ✅
- **Parola politikası güçlü:** uzunluk 8 + digit + uppercase + non-alphanumeric (`DependencyInjection.cs:39-43`). ✅
- **Swagger yalnızca Development'ta:** `Program.cs:54-58`. ✅

---

## Öncelik Sırasına Göre Eylem Listesi (Top Fixes)

1. **[High] Seed admin parolası sabit fallback'ini kaldır** (`DbSeeder.cs:35`) — en kolay ve en yüksek etkili düzeltme; production'da bilinen parolayla Admin ele geçirme riski.
2. **[High] HTTPS redirect + HSTS + güvenlik header'ları ekle** (`Program.cs`).
3. **[High] JWT secret uzunluğunu (>= 32 byte) startup'ta doğrula** (`AuthenticationExtensions.cs`).
4. **[High] Frontend token saklamayı sertleştir** — refresh token'ı HttpOnly cookie'ye, access token'ı in-memory'ye taşı (ayrı geliştirme görevi).
5. **[Medium] Login brute force korumasını kullanıcı bazına da bağla** — tercihen ASP.NET Identity lockout (`AccessFailedCount`) etkinleştir.
6. **[Medium] `AllowedHosts`'u production'da domain ile sınırla.**
7. **[Low] Register validator'a parola karmaşıklık kuralları + Role beyaz listesi ekle.**
8. **[Low] Süresi dolmuş refresh token'lar için retention/temizleme job'u.**
9. **[Low] CORS method/header'larını daralt (opsiyonel).**

---

*Bu rapor salt-okunur denetimdir; hiçbir kod dosyası değiştirilmemiş veya oluşturulmamıştır. Tek çıktı bu rapor dosyasıdır.*
