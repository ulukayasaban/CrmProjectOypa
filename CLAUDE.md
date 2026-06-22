# OYPA CRM — Proje Talimatı (CLAUDE.md)

Bu proje **.NET 9 ASP.NET Core Web API** backend ve **React 19 + TypeScript + Vite** frontend içeren, büyüyebilir bir full-stack CRM uygulamasıdır. OYPA (Oyak Pazarlama) satış/pazarlama ekibinin lead, müşteri, görüşme ve hatırlatma maili yönetimi için kullanılır.

> Referans prototip `oyak-crm-pro/` klasöründedir. **Oraya asla yazma/düzenleme yapma** (sadece UI/UX ve domain referansı için oku).

## Mimari

**Backend — Clean Architecture** (`backend/src`):
- `Domain` — Entities, Enums, ValueObjects, DomainEvents. Hiçbir katmana bağımlı değildir.
- `Application` — Interfaces, Services/UseCases, Validators, Mappings. `Domain` ve `Contracts`'a bağımlıdır.
- `Infrastructure` — Persistence (EF Core), Identity, Repositories, Logging. `Application` + `Domain`'e bağımlıdır.
- `Api` — Controllers, Middlewares, Filters, Extensions, Program.cs. `Application` + `Infrastructure` + `Contracts`'a bağımlıdır.
- `Contracts` — İstek/yanıt DTO'ları. Bağımsızdır.

**Bağımlılık kuralı (ihlal edilemez):**
```
Api → Application → Domain
Infrastructure → Application + Domain
Domain → (hiçbir şey)
```

**Frontend — Feature-based** (`frontend/src`): `app/`, `routes/`, `pages/`, `features/`, `entities/`, `shared/`.

## Kod yazarken kurallar
- SOLID prensiplerine uy.
- Controller içinde iş kuralı yazma.
- Entity'leri doğrudan API response olarak dönme — daima DTO.
- DTO / service / interface / validation ayrımını koru.
- TypeScript `strict` mode'a uygun kod yaz; `any` kullanma.
- Güvenlik, loglama, hata yönetimi ve test edilebilirliği önceliklendir.
- Proje büyüyeceği için kısa vadeli hack çözümler üretme.
- Her önemli değişiklikten önce mimari etkisini açıkla.
- **Kod üretmeden önce hangi dosyaları oluşturacağını listele.**

## Standart API yanıt zarfı
```json
{ "success": true, "message": "İşlem başarılı", "data": {}, "errors": [] }
```

## Komutlar
- Backend build: `dotnet build backend/OypaCrm.sln`
- Backend test: `dotnet test backend/OypaCrm.sln`
- Migration: `dotnet ef migrations add <Ad> -p backend/src/Infrastructure -s backend/src/Api`
- DB güncelle: `dotnet ef database update -p backend/src/Infrastructure -s backend/src/Api`
- API çalıştır: `dotnet run --project backend/src/Api`
- Frontend: `cd frontend && npm install && npm run dev`

## Agent ekibi
`.claude/agents/` altında 8 rol tanımlı: architect, backend, frontend, security, database, qa, code-review, devops. Detaylı süreç ve sorumluluklar için `docs/agent-rules.md`.

## Dokümanlar
- `docs/architecture.md` — katmanlar ve bağımlılıklar
- `docs/api-guidelines.md` — backend best practice
- `docs/frontend-guidelines.md` — frontend best practice
- `docs/coding-standards.md` — isimlendirme ve kod standartları
- `docs/agent-rules.md` — agent ekibi süreci
