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

### Prerequisites — cài trên máy trước khi chạy source

Cần cài **đủ** các mục bắt buộc bên dưới. Sau khi cài, chạy lệnh **Kiểm tra** để xác nhận.

| # | Phần mềm | Phiên bản khuyến nghị | Dùng cho | Tải / cài |
|---|----------|----------------------|----------|-----------|
| 1 | **.NET SDK** | **10.x** (khớp `net10.0` trong repo) | Build & chạy API | [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) |
| 2 | **Node.js** | **20 LTS** trở lên | Frontend Next.js | [https://nodejs.org/](https://nodejs.org/) |
| 3 | **PostgreSQL** | **14+** (16 khuyến nghị) | Database | [https://www.postgresql.org/download/](https://www.postgresql.org/download/) |
| 4 | **Git** | Mới nhất | Clone repo | [https://git-scm.com/downloads](https://git-scm.com/downloads) |
| 5 | **EF Core CLI** | Cùng major với EF trong project (~10.x) | Chạy migration DB | Xem bước cài bên dưới |

**Tuỳ chọn (IDE):** Visual Studio 2022+, VS Code, hoặc Rider.

#### Kiểm tra sau khi cài

```bash
dotnet --version          # ví dụ: 10.0.x
node --version            # ví dụ: v20.x
npm --version
psql --version            # hoặc mở pgAdmin
git --version
```

#### Cài EF Core CLI (chỉ cần một lần trên máy)

```bash
dotnet tool install --global dotnet-ef
# hoặc cập nhật:
dotnet tool update --global dotnet-ef

dotnet ef --version
```

#### PostgreSQL — tạo database

Sau khi cài PostgreSQL, tạo database trống (tên tuỳ bạn, ví dụ `stockledger_retail`):

```sql
CREATE DATABASE stockledger_retail;
```

Ghi nhớ `Host`, `Port`, `Username`, `Password` — dùng ở bước cấu hình API.

#### Cấu hình bắt buộc trên máy dev

| File | Mục đích |
|------|----------|
| `host/StockLedgerRetail.HttpApi.Host/appsettings.json` | Connection string PostgreSQL (`ConnectionStrings:Default`) |
| `frontend/.env.local` | URL API cho UI (copy từ `.env.local.example`) |

**Connection string** (ví dụ):

```json
"ConnectionStrings": {
  "Default": "Host=localhost;Port=5432;Database=stockledger_retail;Username=postgres;Password=YOUR_PASSWORD"
}
```

**Frontend** (`frontend/.env.local`):

```env
NEXT_PUBLIC_API_URL=http://localhost:5270
```

> Không commit mật khẩu thật lên Git. Có thể dùng `appsettings.Development.local.json` (đã nằm trong `.gitignore`) cho cấu hình local.

#### Ghi chú Windows (PowerShell)

- Chạy lệnh từ **thư mục gốc repo** (có folder `host`, `src`, `frontend`).
- Đường dẫn user có dấu `(` — luôn **bọc path trong ngoặc kép** khi `cd`:
  ```powershell
  cd "C:\Users\...\stockledger-retail"
  ```
- Nếu `npm` bị chặn execution policy, dùng: `npm.cmd run dev`
- Chạy API:
  ```powershell
  dotnet run --project host\StockLedgerRetail.HttpApi.Host --launch-profile http
  ```

---

### Chạy lần đầu (từ clone repo)

**1. Clone & vào thư mục project**

```bash
git clone https://github.com/Cuong2000aa/stockledger-retail.git
cd stockledger-retail
```

**2. Cấu hình DB** — sửa `appsettings.json` (hoặc file local) với connection string PostgreSQL của bạn.

**3. Apply migrations**

```bash
cd src/StockLedgerRetail.EntityFrameworkCore
dotnet ef database update --project .
cd ../..
```

**4. Chạy API** (terminal 1)

```bash
dotnet run --project host/StockLedgerRetail.HttpApi.Host --launch-profile http
```

API: [http://localhost:5270](http://localhost:5270) · Swagger: [http://localhost:5270/swagger](http://localhost:5270/swagger)

**5. Chạy Frontend** (terminal 2)

```bash
cd frontend
cp .env.local.example .env.local   # Windows: copy .env.local.example .env.local
npm install
npm run dev
```

UI: [http://localhost:3000/vi](http://localhost:3000/vi) (mặc định tiếng Việt)

---

### Backend (tóm tắt)

```bash
dotnet run --project host/StockLedgerRetail.HttpApi.Host --launch-profile http
```

Cấu hình connection string trong `host/StockLedgerRetail.HttpApi.Host/appsettings.json` (hoặc file `*.local.json`).

Apply migrations:

```bash
cd src/StockLedgerRetail.EntityFrameworkCore
dotnet ef database update --project .
```

### Frontend (tóm tắt)

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
