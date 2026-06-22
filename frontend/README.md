# StockLedger Retail — Web UI

Next.js admin frontend for StockLedger Retail inventory engine.

## Stack

- Next.js 15 (App Router)
- TypeScript
- Tailwind CSS
- next-intl (Vietnamese / English)
- TanStack Query + Axios

## Prerequisites

- Node.js 20+
- StockLedger API running at `http://localhost:5270`

## Setup

```bash
cd frontend
cp .env.local.example .env.local
npm install
npm run dev
```

Open [http://localhost:3000/vi](http://localhost:3000/vi) (default locale: Vietnamese).

Use the language switcher in the sidebar footer to switch **Tiếng Việt / English**.

## Pages

| Route | Description |
|-------|-------------|
| `/[locale]/` | Dashboard — analytics summary, stock by warehouse, movements, low stock |
| `/[locale]/products` | Product CRUD |
| `/[locale]/product-variants` | SKU CRUD (includes CostPrice, SellingPrice, CostSource) |
| `/[locale]/warehouses` | Warehouse CRUD |
| `/[locale]/suppliers` | Supplier CRUD |
| `/[locale]/purchase-orders` | Purchase order list |
| `/[locale]/purchase-orders/new` | Create PO |
| `/[locale]/purchase-orders/[id]` | PO detail — submit / cancel |
| `/[locale]/purchase-orders/[id]/receive` | Create goods receipt from PO |
| `/[locale]/goods-receipts/[id]` | GR detail — approve / cancel |
| `/[locale]/inventory-documents` | Document list |
| `/[locale]/inventory-documents/new/stock-in` | Create stock in |
| `/[locale]/inventory-documents/new/stock-out` | Create stock out |
| `/[locale]/inventory-documents/new/adjustment` | Create adjustment |
| `/[locale]/inventory-documents/new/transfer` | Create transfer |
| `/[locale]/inventory-documents/new/stock-count` | Create stock count |
| `/[locale]/inventory-documents/[id]` | Document detail — approve / cancel |
| `/[locale]/inventory-documents/[id]/edit` | Edit draft document |
| `/[locale]/current-stocks` | Current stock |
| `/[locale]/stock-transactions` | Stock ledger history |

## API

Set `NEXT_PUBLIC_API_URL` in `.env.local` (default: `http://localhost:5270`).

POS integration endpoints are backend-only (`/api/integration/sales/*`); not exposed in this UI.
