# Mimari (Architecture)

## Genel bakış
OYPA CRM, Clean Architecture backend + feature-based React frontend'den oluşan bir monorepo'dur.

```
CrmProject2/
├─ backend/                # .NET 9 çözümü (OypaCrm.sln)
│  ├─ src/
│  │  ├─ Domain/           # Entities, Enums, ValueObjects, Events
│  │  ├─ Application/      # Interfaces, Services, Validators, Mappings
│  │  ├─ Infrastructure/   # EF Core, Identity, Repositories, JWT, Logging
│  │  ├─ Api/              # Controllers, Middlewares, Filters, Program.cs
│  │  └─ Contracts/        # İstek/yanıt DTO'ları
│  └─ tests/
│     ├─ UnitTests/
│     └─ IntegrationTests/
├─ frontend/               # React 19 + TS + Vite (feature-based)
├─ docs/                   # Bu klasör
├─ .claude/agents/         # Agent ekibi rolleri
└─ oyak-crm-pro/           # Referans prototip — DEĞİŞTİRİLMEZ
```

## Katman bağımlılıkları
```
        ┌───────────────┐
        │      Api       │ ──┐
        └───────┬────────┘   │
                ▼            ▼
        ┌───────────────┐  ┌─────────────────┐
        │  Application   │  │  Infrastructure │
        └───────┬────────┘  └────────┬────────┘
                ▼                    ▼
        ┌──────────────────────────────────┐
        │              Domain               │
        └──────────────────────────────────┘
```

- **Domain**: saf iş modeli. Hiçbir paket/katman bağımlılığı yok (EF, ASP.NET vb. YOK).
- **Application**: use-case'ler, arayüzler (repository, IUnitOfWork, IJwtService, ICurrentUser), validator'lar. `Domain` + `Contracts`.
- **Infrastructure**: `Application` arayüzlerinin somut uygulamaları — EF Core `DbContext`, repository'ler, Identity, JWT üretimi, refresh token saklama.
- **Api**: HTTP sınırı. Controller'lar Application servislerini çağırır. Middleware, filtreler, DI kompozisyonu.
- **Contracts**: dışarıya açılan DTO sözleşmeleri. Hem Application hem Api tüketir.

## Temel kurallar
1. Bağımlılıklar yalnızca içe doğru akar (dıştaki katman içtekini bilir).
2. Domain entity'leri API'den dönülmez; `Contracts` DTO'larına map'lenir.
3. Application, EF Core'a doğrudan bağımlı olmaz — arayüzler üzerinden çalışır.
4. Cross-cutting (logging, exception, auth) Api katmanında middleware/filtre olarak.

## Modüller (domain)
`oyak-crm-pro` prototipinden türetilmiştir:
- **Auth/Identity** — ApplicationUser, roller, JWT + refresh token.
- **SalesRep** — OYPA satış temsilcileri.
- **Lead** — potansiyel firmalar (Yeni/İletişimde/...). Müşteriye dönüştürülebilir.
- **Customer** — aktif müşteri portföyü.
- **Contact** — firmaya bağlı ilgili kişiler.
- **Meeting** — randevu/görüşme (planlandı/yapıldı), mail taslağı tetikler.
- **MailDraft** — otomatik hatırlatma maili taslakları (simülasyon).
- **Notification** — sistem bildirimleri.
- **Target** — haftalık görüşme hedefi.

## Veri akışı (örnek: görüşme planlama)
1. `POST /api/meetings` → `MeetingsController` → `IMeetingService.ScheduleAsync(dto)`
2. Servis: validasyon → `Meeting` entity oluştur → `MailDraft` üret → `IUnitOfWork.SaveChangesAsync()`
3. `MeetingDto` map'lenip `ApiResponse<MeetingDto>` ile dönülür.
