# OYPA CRM — Dağıtım, Yedekleme & Geri Alma Stratejisi

Bu doküman production dağıtımında veri güvenliği için izlenecek adımları, veritabanı
yedekleme ve migration geri alma stratejisini açıklar.

## 1) Migration uygulama modeli

Migration'lar API açılışında otomatik uygulanır: `Program.cs` → `DbSeeder.SeedAsync` →
`Database.MigrateAsync()`. Bu kolaylık sağlar ama production'da **kontrolsüz şema değişimi
riski** taşır. Önerilen güvenli akış:

1. **Her dağıtımdan ÖNCE veritabanı yedeği al** (bkz. §2).
2. Tercihen migration'ı önce bir **test/staging** ortamında çalıştır.
3. İstenirse otomatik migrate'i kapatıp migration'ları elle uygula:
   ```bash
   dotnet ef database update -p backend/src/Infrastructure -s backend/src/Api
   ```
4. Yeni migration'lar çoğunlukla **eklemeli** (nullable kolon, yeni tablo) olduğundan
   geriye dönük uyumludur; yine de kolon silme/yeniden adlandırma içeren migration'larda dikkatli ol.

## 2) Veritabanı yedekleme (SQL Server)

**Dağıtımdan önce tam yedek:**
```sql
BACKUP DATABASE [OyakCrmDb]
TO DISK = N'C:\Backups\OyakCrmDb_YYYYMMDD_HHMM.bak'
WITH FORMAT, COMPRESSION, STATS = 10;
```

**Düzenli yedek (önerilen):** Günlük tam + saatlik log yedeği (SQL Server Agent job ya da
Windows Zamanlanmış Görev). Yedekleri farklı bir diskte/sunucuda sakla.

**Geri yükleme:**
```sql
RESTORE DATABASE [OyakCrmDb]
FROM DISK = N'C:\Backups\OyakCrmDb_YYYYMMDD_HHMM.bak'
WITH REPLACE, RECOVERY;
```

## 3) Migration geri alma (rollback)

EF Core code-first'te geri alma iki yolla yapılır:

- **Yedekten geri yükleme (önerilen, en güvenli):** Sorunlu migration sonrası §2'deki
  `.bak` dosyasından restore et. Veri kaybı = son yedekten bu yana yapılan değişiklikler.
- **Belirli bir migration'a geri dönme:** Hedef migration'a `Down` uygulayarak:
  ```bash
  dotnet ef database update <ÖncekiMigrationAdı> -p backend/src/Infrastructure -s backend/src/Api
  ```
  > Uyarı: `Down` adımı kolon/tablo düşürebilir → veri kaybı. Yalnızca yeni eklenen ve henüz
  > veri yazılmamış şema değişiklikleri için güvenlidir.

## 4) Soft-delete & audit (veri güvenliği)

- **Soft-delete:** Company/Contact/Tender/Goal/Meeting/Employee kayıtları fiziksel silinmez;
  `IsDeleted=true` + `DeletedAtUtc` işaretlenir, global query filter ile gizlenir. Yanlışlıkla
  silmede `Restore()` ile geri alınabilir (gerekirse DB'de `UPDATE ... SET IsDeleted=0`).
- **Audit trail:** Tüm Created/Updated/Deleted işlemleri `AuditLogs` tablosuna kullanıcı + zaman
  damgasıyla yazılır (`AuditSaveChangesInterceptor`). "Kim ne zaman değiştirdi" sorgulanabilir:
  ```sql
  SELECT TOP 100 * FROM AuditLogs ORDER BY TimestampUtc DESC;
  ```

## 5) Production yapılandırma kontrol listesi

- `appsettings.Production.json` (gitignored) — `appsettings.Production.json.example`'dan üretilir:
  - **ConnectionStrings:DefaultConnection** — gerçek DB + `api_user` parolası.
  - **Jwt:Secret** — ≥32 karakter güçlü secret (startup'ta uzunluk doğrulanır).
  - **Email** — SMTP bilgileri. `Host` boş bırakılırsa e-posta gönderimi devre dışı (loglanır).
  - **App:FrontendBaseUrl** — parola sıfırlama linkleri için frontend adresi.
  - **Cors:AllowedOrigins** — frontend origin(ler)i.
  - **AllowedHosts** — `*` yerine gerçek API domain'i (ör. `api.oyak.com.tr`) → host header injection riskini azaltır.
- **Seed parolaları:** Org hesapları `Oypa!2026` ile oluşur (demo). Production'da **ilk girişten sonra
  değiştirin** (artık `/auth/change-password` mevcut) veya `Seed__AdminPassword` env değişkeniyle yönetin.
- **.NET 9 Hosting Bundle** + App Pool "No Managed Code" (IIS). Frontend için **URL Rewrite Module**.

## 6) Arka plan işleri (otomatik)

API çalışırken şu periyodik işler devrededir (ek kurulum gerekmez):
- **UpcomingTenderReminder** — yaklaşan ihale bildirimleri (saatlik).
- **WeeklyGoalSnapshot** — hedef haftalık snapshot'ları (6 saatte bir).
- **RefreshTokenCleanup** — 30 günden eski süresi dolmuş refresh token'ları temizler (günlük).

## 7) Gözlemlenebilirlik

- Her isteğe **`X-Correlation-Id`** atanır ve yanıt header'ında döner; loglar bu id ile ilişkilendirilir.
  Hata bildiriminde kullanıcıdan bu id istenebilir.
- **`GET /health`** — liveness + DB hazırlık kontrolü (orchestration/monitoring probe'u için).
