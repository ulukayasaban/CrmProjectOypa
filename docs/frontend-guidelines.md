# Frontend Kuralları

## Genel
- TypeScript `strict` mode açık. `any` yasak; ortak tipler `shared/types` ve `entities/*/model` altında.
- API çağrıları bileşen içinde doğrudan yapılmaz: `shared/api/httpClient.ts` → `features/*/api/*.ts`.
- Token yönetimi tek yerde; access token bellekte (+ opsiyonel `localStorage`), refresh akışı interceptor'da otomatik.
- Route guard (`routes/ProtectedRoute.tsx`) ile korumalı sayfalar.
- Form validasyonları merkezi/yeniden kullanılabilir (zod + react-hook-form önerilir).
- Büyük bileşenler küçük parçalara ayrılır; global state yalnızca gerçekten gerekiyorsa (auth, kullanıcı).
- Feature bazlı klasörleme.

## Klasör yapısı
```
src/
  app/            # uygulama kabuğu, providers, App.tsx
    providers/
  routes/         # router.tsx, ProtectedRoute.tsx
  pages/          # LoginPage, DashboardPage, LeadsPage, ...
  features/       # auth, leads, customers, meetings, drafts, ... (api + ui + model)
  entities/       # user, lead, customer, contact, meeting (tipler + saf yardımcılar)
  shared/
    api/          # httpClient.ts, token storage, interceptors
    components/    # ortak UI (Button, Modal, Table, Badge, ...)
    hooks/
    lib/          # validation şemaları, yardımcılar
    types/
    constants/
```

## API katmanı örneği
```
shared/api/httpClient.ts          # merkezi fetch/axios + auth interceptor
features/auth/api/authApi.ts      # login, refresh, me
features/leads/api/leadApi.ts     # list, create, getById, convert
```

## State
- Sunucu durumu için TanStack Query (önerilir) veya basit fetch hook'ları.
- Auth durumu için tek bir context/provider.

## Stil
- `oyak-crm-pro` prototipinin görsel dili (glassmorphism, lacivert + altın OYPA teması) korunur. CSS değişkenleri referans alınır.

## Kalite
- `npm run build` ve `tsc --noEmit` hatasız geçmeli.
- ESLint kuralları ihlal edilmez.
