# OYPA CRM — E2E Testleri (Playwright)

Kritik kullanıcı akışları için uçtan uca test iskeleti. CI'ı ağırlaştırmamak için
Playwright bağımlılığı `package.json`'a **eklenmemiştir**; kullanmak için kurulum gerekir.

## Kurulum

```bash
cd frontend
npm i -D @playwright/test
npx playwright install chromium
```

## Çalıştırma

Önce API (5022) ve frontend dev server (5173) ayakta olmalı:

```bash
# 1) API
dotnet run --project backend/src/Api --launch-profile http
# 2) Frontend (ayrı terminal)
cd frontend && npm run dev
# 3) E2E (ayrı terminal)
cd frontend && npx playwright test --config e2e/playwright.config.ts
```

## Kapsam (başlangıç)

- `login.spec.ts` — geçerli kimlik bilgisiyle giriş → dashboard'a yönlenme; geçersiz bilgide hata.

Yeni akışlar eklendikçe (ihale oluşturma, sayfalama, bildirim) buraya `*.spec.ts` eklenir.

## CI

İstenirse `.github/workflows/ci.yml`'e ayrı bir `e2e` job eklenir:
servisleri başlat → `npx playwright install --with-deps chromium` → `npx playwright test`.
Tarayıcı indirme maliyeti nedeniyle varsayılan CI'da kapalıdır.
