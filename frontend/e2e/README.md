# OYPA CRM — E2E Testleri (Playwright)

Kritik kullanıcı akışları için uçtan uca testler. Playwright bağımlılığı (`@playwright/test`)
`devDependencies`'tedir; tarayıcılar ayrıca indirilmelidir.

## Kurulum (ilk kez)

```bash
cd frontend
npm install
npx playwright install chromium
```

## Çalıştırma

```bash
cd frontend
npm run test:e2e
```

`playwright.config.ts` `webServer` ile API (5022) ve frontend dev (5173) zaten çalışıyorsa
**yeniden kullanır**, çalışmıyorsa **otomatik başlatır**. Yani sadece komutu çalıştırmak yeterli.

## Kapsam

| Dosya | Akış |
|---|---|
| `login.spec.ts` | Giriş (geçerli→dashboard, geçersiz→login'de kalır) |
| `navigation.spec.ts` | Tüm ana sayfaların yüklenmesi + sidebar navigasyon + grup genişleme |
| `tenders.spec.ts` | İhale arama, sektör filtresi, kazanılan segment |
| `lists.spec.ts` | Lead/Müşteri/Görüşme/Personel arama kutuları + boş durum |
| `calendar.spec.ts` | Takvim ızgarası + ay navigasyonu |
| `notifications.spec.ts` | Bildirim çanı popover |
| `profile.spec.ts` | Profil bilgisi + Düzenle/Parola modalları |
| `account.spec.ts` | Şifremi unuttum linki + forgot/reset sayfaları (anonim) |
| `reports.spec.ts` | Rapor indirme butonları |

`helpers.ts` (login yardımcısı) ve `fixtures.ts` (otomatik giriş yapan `page` fixture) ortak yapılardır.

> Not: Development ortamında rate limiting gevşektir (E2E'nin tekrarlı login/refresh'i engellenmesin
> diye); production'da sıkı limitler korunur.

## CI

`.github/workflows/ci.yml` içinde **`e2e`** job'u olarak tanımlıdır. Maliyetli olduğundan
(SQL Server servis konteyneri + tarayıcı indirme) her push/PR'da değil, **yalnızca elle**
(`workflow_dispatch`) çalışır — GitHub → Actions → CI → "Run workflow".

Job şunları yapar: `mcr.microsoft.com/mssql/server:2022` servis konteyneri ayağa kaldırır,
`ConnectionStrings__DefaultConnection` env'i ile API'yi bu SQL'e bağlar (LocalDB yok),
`npx playwright install --with-deps chromium` ile tarayıcıyı kurar ve `npm run test:e2e`
çalıştırır (Playwright `webServer` API'yi ve frontend'i kendisi başlatır). Hata olursa
`test-results/` izleri artifact olarak yüklenir.
