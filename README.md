# StockLedger Retail

StockLedger Retail is a retail inventory engine built on a **ledger-based** approach: every stock movement is recorded as a `StockTransaction`, and `CurrentStock` is derived from approved transactions.

The project targets real retail scenarios — multi-warehouse stock, procurement, POS integration, and future inventory decision support.

---

## Core Concept

```
InventoryDocument (Draft → Approved)
        ↓
InventoryDocumentLine
        ↓
StockTransaction  ← source of truth for audit
        ↓
CurrentStock      ← fast lookup
```

**Golden rule:** never update `CurrentStock` without a `StockTransaction`.

---

## Implementation Status

| Phase | Scope | Status |
|-------|--------|--------|
| **Phase 1** | Product, SKU, Warehouse, Stock In/Out, Adjustment, Current Stock, Stock History, POS integration | ✅ Done |
| **Phase 2** | Transfer, Stock Count, Update Draft document | ✅ Done |
| **Phase 3** | Supplier, Purchase Order, Goods Receipt, Analytics dashboard | ✅ Done |
| **Sprint 2** | Concurrency (xmin), WAC valuation, stock reconciliation | ✅ Done |
| **Omni-channel** | Multi-warehouse ATP, allocate warehouse, stock reservation | ✅ Done |
| **Multi-brand (MB-1→4)** | Brand entity, transfer policy, in-transit transfer, brand-scoped insights & fulfillment, scope headers | ✅ Done |
| **RBAC** | Email users, permission groups in DB, teams, document authorization | ✅ Done |
| **Login (stub)** | `POST /api/auth/login`, frontend `/login`, session → `X-User-Email` header | ✅ Done |
| **Valuation** | CostPrice, SellingPrice, CostSource on SKU; ProductCostHistory entity | ✅ Domain + DB |
| **Insights** | Dead stock, sales velocity, transfer suggestions (rule-based) | ✅ Done |
| **AI Copilot** | Natural-language Q&A on insight APIs | 🔜 Planned |

---

## Features (Implemented)

### Master Data

- **Brand** — multi-brand master (`Code`, `Name`, `Status`); scopes products, SKUs, and warehouses
- **Product** — parent product (code, name, brand text, optional `BrandId`, category)
- **ProductVariant (SKU)** — actual inventory unit; optional `BrandId`; SKU unique per `(BrandId, Sku)`
- **Warehouse** — DC, Store, Sub-warehouse, Defect, Return, **InTransit**; hierarchy via `ParentWarehouseId`; optional `BrandId`, `RegionCode`, `FulfillmentPriority`
- **Supplier** — procurement partner master data
- **TransferPolicy** — rules for cross-brand transfers

### Inventory Documents

All documents start as **Draft**; stock changes only after **Approve**.

| Type | API route | Effect on stock |
|------|-----------|-----------------|
| Stock In | `POST /api/inventory-documents/stock-in` | +IN |
| Stock Out | `POST /api/inventory-documents/stock-out` | -OUT |
| Adjustment | `POST /api/inventory-documents/adjustment` | +/- ADJUSTMENT |
| Transfer | `POST /api/inventory-documents/transfer` | Ship on approve: OUT source + IN in-transit |
| Stock Count | `POST /api/inventory-documents/stock-count` | COUNT_ADJUSTMENT if variance ≠ 0 |

**Transfer (in-transit):** `POST .../approve` ships to in-transit warehouse; `POST .../{id}/receive-transfer` completes receipt at destination.

Additional: `PUT /api/inventory-documents/{id}` (update draft), `POST .../approve`, `POST .../receive-transfer`, `POST .../cancel`.

### Procurement

```
Supplier → Purchase Order (Draft → Submitted)
              ↓
         Goods Receipt (Draft → Approved)
              ↓
         Stock In document (auto-created & approved)
              ↓
         CurrentStock updated; PO received qty updated
```

### POS & Omni-Channel Integration

`POST /api/integration/sales/check-availability` — read-only stock check  
`POST /api/integration/sales/confirm-sale` — create + approve Stock Out (idempotent)  
`POST /api/integration/sales/confirm-return` — create + approve Stock In (idempotent)  
`POST /api/integration/fulfillment/check-availability-multi-warehouse` — ATP across warehouses (optional `brandId`, `regionCode`)  
`POST /api/integration/fulfillment/allocate-warehouse` — auto-select ship-from warehouse  

Optional scope headers (RBAC-lite): `X-Brand-Id`, `X-Warehouse-Ids`, `X-Region-Code`.

### Authorization (email + DB permissions)

- **Login:** `POST /api/auth/login` (stub: `admin` / `1234`) → frontend session; API calls send `X-User-Email`
- Identify users via header `X-User-Email` (registered in `app_users`)
- Permission groups: `SYSTEM_ADMIN`, `TEAM_LEADER`, `WAREHOUSE_CLERK`, `VIEWER`
- Team leaders can update/cancel/approve documents created by team members
- Admin APIs: `/api/admin/users`, `/api/admin/permissions`, `/api/admin/teams`, `GET /api/auth/me`

See [docs/RBAC.md](docs/RBAC.md).

### Brands

- `GET/POST /api/brands`, `GET/PUT /api/brands/{id}`

### Inventory Insights (read-only)

- `GET /api/inventory-insights/dead-stock`
- `GET /api/inventory-insights/sales-velocity`
- `GET /api/inventory-insights/transfer-suggestions` — filter by `brandId`, `regionCode`

### Analytics (read-only)

- `GET /api/analytics/summary` — totals, open POs, pending GRs
- `GET /api/analytics/stock-by-warehouse`
- `GET /api/analytics/movements` — in/out over date range
- `GET /api/analytics/low-stock` — SKUs below threshold

### Frontend (Next.js)

Bilingual UI (VI / EN): login, dashboard, products, SKUs, warehouses, suppliers, purchase orders, goods receipts, inventory documents, current stock, stock history, insights.

Default locale: `vi` — `http://localhost:3000/vi`

**Dev tip:** if Next.js shows Internal Server Error after `npm run build` while dev is running, run `npm run dev:fresh` in `frontend/` (clears `.next` cache).

---

## Architecture

Clean Architecture layers:

```
host/StockLedgerRetail.HttpApi.Host     ← ASP.NET host
src/StockLedgerRetail.HttpApi           ← Controllers
src/StockLedgerRetail.Application       ← App services, StockLedgerService
src/StockLedgerRetail.Application.Contracts
src/StockLedgerRetail.Domain            ← Entities, repository interfaces
src/StockLedgerRetail.Domain.Shared     ← Enums
src/StockLedgerRetail.EntityFrameworkCore
frontend/                               ← Next.js 15 + TypeScript + Tailwind
```

---

## Technology Stack

| Layer | Stack |
|-------|--------|
| Backend | .NET 10, ASP.NET Core Web API, EF Core |
| Database | PostgreSQL |
| Frontend | Next.js 15, TypeScript, Tailwind CSS, next-intl, TanStack Query |
| API docs | Swagger — `http://localhost:5270/swagger` |

---

## Getting Started

### Prerequisites — install before running from source

Install **all** required items below. After installing, run the **Verify** commands to confirm.

| # | Software | Recommended version | Used for | Download |
|---|----------|---------------------|----------|----------|
| 1 | **.NET SDK** | **10.x** (matches `net10.0` in this repo) | Build & run API | [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) |
| 2 | **Node.js** | **20 LTS** or newer | Next.js frontend | [https://nodejs.org/](https://nodejs.org/) |
| 3 | **PostgreSQL** | **14+** (16 recommended) | Database | [https://www.postgresql.org/download/](https://www.postgresql.org/download/) |
| 4 | **Git** | Latest | Clone repo | [https://git-scm.com/downloads](https://git-scm.com/downloads) |
| 5 | **EF Core CLI** | Same major as EF in the project (~10.x) | Database migrations | See install step below |

**Optional (IDE):** Visual Studio 2022+, VS Code, or Rider.

#### Verify after install

```bash
dotnet --version          # e.g. 10.0.x
node --version            # e.g. v20.x
npm --version
psql --version            # or use pgAdmin
git --version
```

#### Install EF Core CLI (one-time per machine)

```bash
dotnet tool install --global dotnet-ef
# or update:
dotnet tool update --global dotnet-ef

dotnet ef --version
```

#### PostgreSQL — create database

After installing PostgreSQL, create an empty database (any name, e.g. `stockledger_retail`):

```sql
CREATE DATABASE stockledger_retail;
```

Note your `Host`, `Port`, `Username`, and `Password` — you will need them when configuring the API.

#### Required local configuration

| File | Purpose |
|------|---------|
| `host/StockLedgerRetail.HttpApi.Host/appsettings.json` | PostgreSQL connection string (`ConnectionStrings:Default`) |
| `frontend/.env.local` | API URL for the UI (copy from `.env.local.example`) |

**Connection string** (example):

```json
"ConnectionStrings": {
  "Default": "Host=localhost;Port=5432;Database=stockledger_retail;Username=postgres;Password=YOUR_PASSWORD"
}
```

**Frontend** (`frontend/.env.local`):

```env
NEXT_PUBLIC_API_URL=http://localhost:5270
```

> Do not commit real passwords to Git. You can use `appsettings.Development.local.json` (already in `.gitignore`) for local secrets.

#### Windows notes (PowerShell)

- Run commands from the **repository root** (folders `host`, `src`, `frontend`).
- If your user path contains `(`, always **wrap paths in quotes** when using `cd`:
  ```powershell
  cd "C:\Users\...\stockledger-retail"
  ```
- If `npm` is blocked by execution policy, use: `npm.cmd run dev`
- Run the API:
  ```powershell
  dotnet run --project host\StockLedgerRetail.HttpApi.Host --launch-profile http
  ```

---

### First run (from a fresh clone)

**1. Clone & enter the project**

```bash
git clone https://github.com/Cuong2000aa/stockledger-retail.git
cd stockledger-retail
```

**2. Configure the database** — edit `appsettings.json` (or a local override) with your PostgreSQL connection string.

**3. Apply migrations** (run from **repository root**):

```bash
dotnet ef database update \
  --project src/StockLedgerRetail.EntityFrameworkCore/StockLedgerRetail.EntityFrameworkCore.csproj \
  --startup-project host/StockLedgerRetail.HttpApi.Host/StockLedgerRetail.HttpApi.Host.csproj
```

Or from the EF project folder:

```bash
cd src/StockLedgerRetail.EntityFrameworkCore
dotnet ef database update --project . --startup-project ../../host/StockLedgerRetail.HttpApi.Host/StockLedgerRetail.HttpApi.Host.csproj
cd ../..
```

**4. Run the API** (terminal 1)

```bash
dotnet run --project host/StockLedgerRetail.HttpApi.Host --launch-profile http
```

API: [http://localhost:5270](http://localhost:5270) · Swagger: [http://localhost:5270/swagger](http://localhost:5270/swagger)

**5. Run the frontend** (terminal 2)

```bash
cd frontend
cp .env.local.example .env.local   # Windows: copy .env.local.example .env.local
npm install
npm run dev
```

UI: [http://localhost:3000/vi](http://localhost:3000/vi) (default locale is Vietnamese; use `/en` for English)

---

### Backend (quick reference)

```bash
dotnet run --project host/StockLedgerRetail.HttpApi.Host --launch-profile http
```

Configure the connection string in `host/StockLedgerRetail.HttpApi.Host/appsettings.json` (or a `*.local.json` file).

Apply migrations (from repo root):

```bash
dotnet ef database update \
  --project src/StockLedgerRetail.EntityFrameworkCore/StockLedgerRetail.EntityFrameworkCore.csproj \
  --startup-project host/StockLedgerRetail.HttpApi.Host/StockLedgerRetail.HttpApi.Host.csproj
```

### Frontend (quick reference)

```bash
cd frontend
npm install
npm run dev
```

Set `NEXT_PUBLIC_API_URL=http://localhost:5270` in `frontend/.env.local` if needed.

---

## Documentation

| File | Content |
|------|---------|
| [docs/RBAC.md](docs/RBAC.md) | Email-based RBAC, permission groups, teams |
| [docs/MultiBrand.md](docs/MultiBrand.md) | Multi-brand phases, in-transit transfer, scope headers (EN) |
| [docs/MultiBrand.vi.md](docs/MultiBrand.vi.md) | Đa thương hiệu (VI) |
| [docs/UseCases.md](docs/UseCases.md) | Use cases UC001–UC016 |
| [docs/BusinessRules.md](docs/BusinessRules.md) | Business rules (EN) |
| [docs/BusinessRules.vi.md](docs/BusinessRules.vi.md) | Business rules (VI) |
| [docs/Entities.md](docs/Entities.md) | Entity dictionary (EN) |
| [docs/Entities.vn.md](docs/Entities.vn.md) | Entity dictionary (VI) |
| [docs/ERD.md](docs/ERD.md) | Database tables & relationships |
| [docs/InventoryDomain.md](docs/InventoryDomain.md) | Domain overview |

---

## Planned

Items below are **not done yet** (or only partially done). See the [Implementation Status](#implementation-status) table for what is already shipped.

| Item | Notes |
|------|--------|
| **JWT / OAuth** | Replace stub login (`admin`/`1234`); keep permissions loaded from DB |
| **AI Copilot** | Natural-language Q&A on top of insight APIs (optional LLM layer) |
| **TransferPolicy admin API** | CRUD UI/API for cross-brand transfer rules (policy is enforced on transfer today; seeded in DB) |
| **Frontend multi-brand admin** | Brand master screens, `BrandId` pickers on product/SKU/warehouse (backend `GET/POST /api/brands` exists) |
| **Docker deployment** | `docker-compose` for API + PostgreSQL + frontend |

---

## Project Status

🚧 **Active development** — Core inventory, omni-channel, multi-brand (MB-1→4), RBAC, stub login, and rule-based insights are **done**. Next: JWT/OAuth, brand/transfer-policy admin UI, AI copilot, Docker.
