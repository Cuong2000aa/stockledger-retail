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

### Prerequisites

- .NET 10 SDK
- Node.js 20+
- PostgreSQL

### Backend

```bash
dotnet run --project host/StockLedgerRetail.HttpApi.Host
```

Configure connection string in `host/StockLedgerRetail.HttpApi.Host/appsettings.json` (not committed).

Apply migrations:

```bash
dotnet ef database update \
  --project src/StockLedgerRetail.EntityFrameworkCore \
  --startup-project host/StockLedgerRetail.HttpApi.Host
```

### Frontend

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
