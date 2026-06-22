# OYPA CRM

OYPA CRM, .NET 9 (Clean Architecture) tabanli bir backend API ile Vite + React + TypeScript (feature-based) bir frontend'den olusan tam yiginli (full-stack) bir musteri iliskileri yonetimi uygulamasidir.

> `oyak-crm-pro/` dizini bu projenin **referans prototipidir**. Yalnizca ornek/ilham amaciyla repoda tutulur ve **degistirilmez**; uretim kodu `backend/` ve `frontend/` altindadir.

---

## Mimari

### Backend - Clean Architecture (`backend/`)

Solution: `backend/OypaCrm.sln` (.NET 9, SDK `global.json` ile `9.0.312`'ye sabit).

| Katman | Proje | Sorumluluk |
|--------|-------|------------|
| Domain | `src/Domain` (`Oypa.Crm.Domain`) | Entity'ler, enum'lar, value object'ler, is kurallari. Bagimliligi yok. |
| Contracts | `src/Contracts` (`Oypa.Crm.Contracts`) | DTO'lar ve disa donuk sozlesmeler. |
| Application | `src/Application` (`Oypa.Crm.Application`) | Servisler, arayuzler, validator'lar, mapping'ler (use-case'ler). |
| Infrastructure | `src/Infrastructure` (`Oypa.Crm.Infrastructure`) | EF Core (SQL Server), Identity, repository'ler, JWT, migration'lar. |
| Api | `src/Api` (`Oypa.Crm.Api`) | Controller'lar, middleware, filter'lar, auth, rate limiting, `Program.cs`. Baslangic projesi. |

Testler: `backend/tests/UnitTests` ve `backend/tests/IntegrationTests`.

### Frontend - Feature-based (`frontend/`)

Vite + React + TypeScript. Ozellikler (feature) bazli klasor yapisi; veri katmani icin TanStack Query, HTTP icin axios, formlar icin react-hook-form + zod, yonlendirme icin react-router-dom.

---

## Onkosullar

- .NET SDK 9.0.312 (`global.json` ile sabit; `rollForward: latestFeature`)
- Node.js 22+ ve npm
- SQL Server (yerel kurulum) **veya** Docker (compose ile birlikte gelir)
- Docker / Docker Compose (konteyner ile calistirma icin)

---

## Yerel Calistirma (Docker'siz)

### Backend

```bash
# 1) Gizli bilgileri user-secrets ile saglayin (repoya secret koymayin):
cd backend/src/Api
dotnet user-secrets set "Jwt:Secret" "<en-az-32-karakterlik-anahtar>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=OypaCrm;Trusted_Connection=True;TrustServerCertificate=True"

# 2) Veritabani semasini olusturun:
dotnet ef database update --project ../Infrastructure --startup-project .

# 3) API'yi calistirin:
dotnet run
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

Frontend, API taban URL'sini `VITE_API_BASE_URL` ortam degiskeninden okur (varsayilan compose degeri: `http://localhost:5080/api`).

---

## Docker Compose ile Calistirma

```bash
# 1) Ortam dosyasini hazirlayin:
cp .env.example .env          # PowerShell: Copy-Item .env.example .env
#   .env icinde MSSQL_SA_PASSWORD ve JWT_SECRET (>= 32 karakter) doldurun.

# 2) Tum yigini ayaga kaldirin:
docker compose up --build
```

Servisler ve portlar:

| Servis | Image / Build | Port (host:container) |
|--------|---------------|------------------------|
| `db` | `mcr.microsoft.com/mssql/server:2022-latest` | `1433:1433` |
| `api` | `backend/src/Api/Dockerfile` (context = repo koku) | `5080:8080` |
| `frontend` | `frontend/Dockerfile` (Nginx) | `5173:80` |

- Frontend: http://localhost:5173
- API: http://localhost:5080
- `api`, `db` saglikli (healthy) olana kadar bekler (`depends_on` + healthcheck).
- Connection string ve JWT secret container'a ortam degiskeni olarak gecer; repoda gizli bilgi tutulmaz.

---

## Seed Admin

Uygulama ilk migration/seed ile bir yonetici hesabi olusturur:

- **E-posta:** `admin@oypa.com.tr`
- **Sifre:** `Admin!23456`

---

## Agent Ekibi ve Dokumantasyon

Bu proje rol-tabanli agent'larla gelistirilmistir. Tanimlar `.claude/agents/` altindadir:

- `architect` - mimari ve katman tasarimi
- `backend` - .NET API ve is mantigi
- `frontend` - React/TS arayuz
- `database` - EF Core ve veri modeli
- `security` - kimlik dogrulama, yetkilendirme, secret yonetimi
- `qa` - test stratejisi ve test kodu
- `code-review` - kod inceleme
- `devops` - Docker, ortam ayrimi, CI/CD

Ayrintili rehberler `docs/` altindadir: `architecture.md`, `coding-standards.md`, `api-guidelines.md`, `frontend-guidelines.md`, `agent-rules.md`.

---

## CI

GitHub Actions is akisi: `.github/workflows/ci.yml`

- **backend** job: `dotnet restore/build/test backend/OypaCrm.sln` (.NET 9.0.x, ubuntu).
- **frontend** job: `npm ci`, `npm run lint`, `npm run build` (Node 22, `frontend/`).
