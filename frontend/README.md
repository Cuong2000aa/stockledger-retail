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
| `/[locale]/` | Dashboard |
| `/[locale]/products` | Product CRUD |
| `/[locale]/product-variants` | SKU CRUD |
| `/[locale]/warehouses` | Warehouse CRUD |
| `/[locale]/inventory-documents` | Document list |
| `/[locale]/inventory-documents/new/stock-in` | Create stock in |
| `/[locale]/inventory-documents/new/stock-out` | Create stock out |
| `/[locale]/inventory-documents/new/adjustment` | Create adjustment |
| `/[locale]/inventory-documents/[id]` | Document detail, approve/cancel |
| `/[locale]/current-stocks` | Current stock |
| `/[locale]/stock-transactions` | Stock ledger history |
