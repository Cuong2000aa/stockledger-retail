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
| **Valuation** | CostPrice, SellingPrice, CostSource on SKU; ProductCostHistory entity | ✅ Domain + DB |
| **Phase 4** | Dead stock, Markdown simulation, Transfer suggestions, AI Copilot | 🔜 Planned |

---

## Features (Implemented)

### Master Data

- **Product** — parent product (code, name, brand, category)
- **ProductVariant (SKU)** — actual inventory unit; optional `CostPrice`, `SellingPrice`, `CostSource`
- **Warehouse** — DC, Store, Sub-warehouse, Defect, Return; hierarchy via `ParentWarehouseId`
- **Supplier** — procurement partner master data

### Inventory Documents

All documents start as **Draft**; stock changes only after **Approve**.

| Type | API route | Effect on stock |
|------|-----------|-----------------|
| Stock In | `POST /api/inventory-documents/stock-in` | +IN |
| Stock Out | `POST /api/inventory-documents/stock-out` | -OUT |
| Adjustment | `POST /api/inventory-documents/adjustment` | +/- ADJUSTMENT |
| Transfer | `POST /api/inventory-documents/transfer` | TRANSFER_OUT + TRANSFER_IN |
| Stock Count | `POST /api/inventory-documents/stock-count` | COUNT_ADJUSTMENT if variance ≠ 0 |

Additional: `PUT /api/inventory-documents/{id}` (update draft), `POST .../approve`, `POST .../cancel`.

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

### POS Integration

`POST /api/integration/sales/check-availability` — read-only stock check  
`POST /api/integration/sales/confirm-sale` — create + approve Stock Out (idempotent)  
`POST /api/integration/sales/confirm-return` — create + approve Stock In (idempotent)

### Analytics (read-only)

- `GET /api/analytics/summary` — totals, open POs, pending GRs
- `GET /api/analytics/stock-by-warehouse`
- `GET /api/analytics/movements` — in/out over date range
- `GET /api/analytics/low-stock` — SKUs below threshold

### Frontend (Next.js)

Bilingual UI (VI / EN): dashboard, products, SKUs, warehouses, suppliers, purchase orders, goods receipts, inventory documents, current stock, stock history.

Default locale: `vi` — `http://localhost:3000/vi`

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

**3. Apply migrations**

```bash
cd src/StockLedgerRetail.EntityFrameworkCore
dotnet ef database update --project .
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

Apply migrations:

```bash
cd src/StockLedgerRetail.EntityFrameworkCore
dotnet ef database update --project .
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
| [docs/UseCases.md](docs/UseCases.md) | Use cases UC001–UC011 |
| [docs/BusinessRules.md](docs/BusinessRules.md) | Business rules (EN) |
| [docs/BusinessRules.vi.md](docs/BusinessRules.vi.md) | Business rules (VI) |
| [docs/Entities.md](docs/Entities.md) | Entity dictionary (EN) |
| [docs/Entities.vn.md](docs/Entities.vn.md) | Entity dictionary (VI) |
| [docs/ERD.md](docs/ERD.md) | Database tables & relationships |
| [docs/InventoryDomain.md](docs/InventoryDomain.md) | Domain overview |

---

## Planned (Phase 4+)

- **Inventory Insights** — dead stock, markdown simulation, transfer suggestions (rule-based, no AI cost)
- **AI Copilot** — natural-language Q&A on top of insight APIs (optional LLM layer)
- Auth / JWT, stock reservation, Docker deployment

---

## Project Status

🚧 **Active development** — Phase 1–3 complete; valuation domain in place; Phase 4 insights planned.
