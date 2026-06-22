# OYPA CRM — Production Kurulum & Sır Yönetimi

Bu doküman, API'nin **production (sunucu)** ortamında çalışması için gereken
yapılandırmayı açıklar. Hassas değerler (DB parolası, JWT secret) **kaynak koda
yazılmaz**; ortam değişkenleriyle verilir. `appsettings.Production.json` yalnızca
**sır içermeyen** production ayarlarını (CORS, AllowedHosts) tutar.

> ASP.NET Core, `ASPNETCORE_ENVIRONMENT` ayarlı değilse ortamı **Production**
> kabul eder ve `appsettings.Production.json` + ortam değişkenlerini otomatik yükler.
> Ortam değişkenleri her zaman `appsettings*.json`'ı **ezer**. İç içe anahtarlar
> çift alt çizgi (`__`) ile verilir: `ConnectionStrings:DefaultConnection` → `ConnectionStrings__DefaultConnection`.

---

## 1) Zorunlu ortam değişkenleri

Sunucuda (Windows) **yönetici** PowerShell/CMD ile makine düzeyinde (`/M`) ayarlayın.
`setx` ileride başlayacak süreçler için geçerlidir; ayarladıktan sonra **API servisini
yeniden başlatın**.

### a) Veritabanı bağlantısı (DB parolası burada — dosyada DEĞİL)
```cmd
setx ConnectionStrings__DefaultConnection "Server=localhost;Database=OyakCrmDb;User Id=api_user;Password=<DB_PAROLANIZ>;TrustServerCertificate=True;MultipleActiveResultSets=True" /M
```
> `<DB_PAROLANIZ>` yerine gerçek parolayı yazın. Parolayı bu dokümana/koda yazmayın.

### b) JWT imza anahtarı
JWT secret artık **`appsettings.Production.json`** içinde `Jwt:Secret` olarak tutulmaktadır
(kullanıcı tercihi). Development için `appsettings.Development.json`'da ayrı bir secret vardır.
Ek bir ortam değişkeni gerekmez.
> ⚠️ **Güvenlik:** Production secret bir dosyada düz metin durduğundan, repo/yedek
> sızması riski taşır. Bu dosya bir yere kopyalanır/commit'lenirse secret'ı **rotate edin**.
> Daha güvenli isterseniz dosyadaki değeri silip ortam değişkeniyle ezebilirsiniz:
> `setx Jwt__Secret "<güçlü-32+karakter>" /M` (ortam değişkeni `appsettings`'i ezer).

### c) (Opsiyonel) Production admin tohum parolası
Base admin (`admin@oypa.com.tr`) production'da yalnızca bu değer verilirse oluşturulur:
```cmd
setx Seed__AdminPassword "<güçlü-admin-parolası>" /M
```
Verilmezse base admin oluşturulmaz (log'a uyarı düşer).

---

## 2) CORS & Host ayarları (`appsettings.Production.json`)

Dosyadaki placeholder'ı **gerçek frontend adresinizle** değiştirin:
```json
{
  "Cors": { "AllowedOrigins": [ "https://FRONTEND-ADRESINIZI-YAZIN" ] },
  "AllowedHosts": "*"
}
```
- `Cors:AllowedOrigins` → tarayıcıdan API'ye erişen **frontend origin'i** (ör. `https://crm.oyak.com.tr`). Birden fazla olabilir.
- Alternatif olarak ortam değişkeniyle de verilebilir: `setx Cors__AllowedOrigins__0 "https://crm.oyak.com.tr" /M`
- `AllowedHosts` → API'nin yanıt verdiği host. Reverse proxy (IIS/Nginx) arkasında `*` güvenle kullanılabilir; istenirse API domain'ine daraltın (ör. `api.oyak.com.tr`).

---

## 3) Veritabanı ön koşulları

`api_user` genelde sınırlı yetkili olduğundan, dağıtımdan önce bir DBA şunları yapmalı:
1. `OyakCrmDb` veritabanını oluştur (boş).
2. `api_user` login'ini oluştur (parola yukarıdaki ile aynı) ve `OyakCrmDb` üzerinde `db_owner` ver (migration'lar tablo oluşturup değiştirebilsin).
3. SQL Server'da **SQL Server Authentication** (mixed mode) açık olmalı (User Id/Password ile bağlanılıyor).

> **Migration'lar otomatik uygulanır:** API açılışında `DbSeeder.SeedAsync` →
> `Database.MigrateAsync()` çağrılır; bekleyen tüm migration'lar `OyakCrmDb`'ye uygulanır
> ve başlangıç verisi (organizasyon, örnekler) tohumlanır. Manuel istenirse:
> `dotnet ef database update -p backend/src/Infrastructure -s backend/src/Api`
> (bağlantı dizesi env değişkeninden okunur).

---

## 4) Dağıtım sırası (özet)

1. SQL Server hazır + `OyakCrmDb` + `api_user` (db_owner) — bkz. §3.
2. Sunucuda env değişkenlerini ayarla — bkz. §1 (DB, JWT, ops. admin).
3. `appsettings.Production.json` içinde CORS frontend adresini gerçek değerle değiştir — bkz. §2.
4. API'yi yayımla: `dotnet publish backend/src/Api -c Release -o <yayın-klasörü>`
5. Servisi/uygulamayı (yeniden) başlat → migration'lar otomatik uygulanır, API ayağa kalkar.
6. Doğrula: `https://<api-host>/swagger` (Swagger yalnız Development'ta açıktır; production'da kapalıdır — sağlık için bir endpoint'e istek atın).

---

## 5) Güvenlik notları / öneriler

- 🔐 DB parolası ve JWT secret **yalnız ortam değişkeninde**; `appsettings*.json`'da değil.
- ⚠️ **Organizasyon tohum parolası:** `DbSeeder.SeedOrgAsync` içinde org hesapları (Umur, Avniye, vb.) için parola `Oypa!2026` **sabit** kodludur. Production'da bu zayıf/bilinen bir parola olur. Öneri: ya production'da bu hesapların parolalarını ilk girişten sonra değiştirin, ya da org tohum parolasını da bir env değişkeninden okunur hale getirelim (istenirse yapılır).
- Bu repo git'e eklenirse: `appsettings.Production.json` artık sır içermiyor (commit'lenebilir). Yine de sır içeren herhangi bir dosya `.gitignore`'a eklenmeli.
- HTTPS/HSTS production'da etkin (`UseHsts`); reverse proxy'de TLS sonlandırması önerilir.
