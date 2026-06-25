# Inventory Domain

Overview of the retail inventory domain as implemented in StockLedger Retail.

---

## Core Entities

### Brand

- **Brand** — master for multi-brand retail (`Code`, `Name`, `Status`).
- Products, SKUs, and warehouses may reference `BrandId`.
- SKU uniqueness is scoped: `(BrandId, Sku)`.

### TransferPolicy

- Controls **cross-brand** warehouse transfers (`AllowCrossBrand`, optional source/destination brand).
- Same-brand transfers do not require a policy row.

### Product & ProductVariant (SKU)

- **Product** — parent master data (code, name, brand text, optional `BrandId`, category).
- **ProductVariant** — the actual inventory unit. All stock is tracked at SKU level. Optional `BrandId`.
- **TrackLotExpiry** — when `true`, SKU uses `StockLot` / `LotStock` and FEFO for outbound allocation.
- **Current cache fields** on SKU:
  - `CurrentCostPrice` — current operational cost cache
  - `CurrentSellingPrice` — current operational selling price cache
  - `CurrentSellingPriceBeforeVat` / `CurrentSellingPriceAfterVat`
  - `VatRate`
  - `CurrentCostSource`
- **Legacy compatibility fields** remain on SKU:
  - `CostPrice`
  - `SellingPrice`
  - `CostSource`
- **ProductPrice** — effective-dated selling price rows by `PriceType` (`Regular`, `Promotion`, `Markdown`, `Clearance`, `Channel`)
- **ProductCostHistory** — effective-dated cost records with source, valuation method, reference metadata
- **InventoryValuationSnapshot** — persisted valuation snapshot by SKU / warehouse / date for reporting and analytics

### StockLot & LotStock

Batch/lot tracking for expiry-sensitive SKUs.

- **StockLot** — `LotCode`, `ExpiryDate`, `ProductVariantId`
- **LotStock** — on-hand quantity per lot per warehouse
- **LotStockService** — FEFO pick when deducting lot-tracked stock

### Warehouse

Physical or logical storage location: DC, Store, Sub-warehouse, Defect, Return, **InTransit**. Supports parent-child hierarchy. Optional `BrandId`, `RegionCode`, `FulfillmentPriority` for omni-channel and replenishment.

### CurrentStock

Fast lookup of on-hand quantity per SKU per warehouse.

```
QuantityAvailable = QuantityOnHand - QuantityReserved
```

`QuantityReserved` is updated by POS/OMS reservation APIs; admin can list and release holds via `/api/stock-reservations`.

### InventoryDocument

Business document header. Types: `StockIn`, `StockOut`, `Transfer`, `Adjustment`, `StockCount`.

Statuses: `Draft` → `Approved` (or `Cancelled` while Draft). Transfer documents may become `Completed` after receive.

Transfer lifecycle (type `Transfer`): **Approve** = ship (source → in-transit); **Receive** = in-transit → destination.

Fields: `TransferLifecycleStatus`, `InTransitWarehouseId`, `ShippedAt`, `ReceivedAt`.

**Approval workflow:** `RequiredApprovalSteps`, `CompletedApprovalSteps`, `SubmittedForApprovalAt`. High-value documents use `submit-for-approval` before approve.

Only **approved** documents generate `StockTransaction` records (transfer receive generates additional transactions).

### StockTransaction

Immutable ledger entry for every inventory movement. Links to document, document line, SKU, and warehouse.

Transaction types: `In`, `Out`, `TransferIn`, `TransferOut`, `AdjustmentIn`, `AdjustmentOut`, `CountAdjustmentIn`, `CountAdjustmentOut`.

---

## Procurement Domain

### Supplier

Vendor master data for purchase operations.

### PurchaseOrder

Order to supplier. Status flow:

```
Draft → Submitted → PartiallyReceived → Received
                 ↘ Cancelled (if no goods received)
```

Does **not** affect stock until goods are received via Goods Receipt.

### GoodsReceipt

Physical receipt against a submitted PO. On **approve**:

1. Creates and approves a `StockIn` inventory document (`SourceSystem = PROCUREMENT`)
2. Updates `ReceivedQuantity` on PO lines
3. Updates PO status (`PartiallyReceived` or `Received`)

---

## POS & Omni-Channel Integration

External sales systems (POS, OMS, marketplaces) call integration APIs instead of managing stock directly.

- **Check availability** — read-only (single warehouse)
- **Multi-warehouse ATP** — optional `brandId`, `regionCode`
- **Allocate warehouse** — ship-from-store / DC selection with brand scope
- **Confirm sale** — Stock Out, auto-approved, idempotent by `sourceSystem + orderReference`
- **Confirm return** — Stock In, auto-approved, idempotent by `sourceSystem + returnReference`

Optional API scope headers: `X-Brand-Id`, `X-Warehouse-Ids`, `X-Region-Code`.

---

## Inventory Insights (Read-Only)

Pricing-aware, rule-based decision support — **read-only**, no stock posting.

**Executive layer:** `GET /api/inventory-insights/executive-summary` aggregates KPIs (dead stock value, velocity count, transfer/markdown/promotion/reorder risk, trend deltas).

**Seven analytics views:**

| View | Endpoint |
|------|----------|
| Dead stock | `.../dead-stock` |
| Sales velocity | `.../sales-velocity` |
| Transfer suggestions | `.../transfer-suggestions` |
| Markdown candidates | `.../markdown-candidates` |
| Promotion risk | `.../promotion-risk` |
| Reorder risk | `.../reorder-risk` |
| Trend summary | `.../trend-summary` |

DTOs include selling price (before/after VAT), cost, margin, and inventory value from `ProductPrice`, SKU cache, and `InventoryValuationSnapshot`. `InsightRecommendationEngine` attaches drill-down CTAs (stock history, SKU, reports, draft transfer/PO).

Filterable by `warehouseId`, `brandId`, `regionCode`. Results may be served from `InsightSnapshot` cache (refresh via admin operations).

Full reference: [Insights.md](Insights.md) · [Insights.vi.md](Insights.vi.md) · [MultiBrand.md](MultiBrand.md).

---

## Inventory Reports (Read-Only)

- Inventory value — latest `InventoryValuationSnapshot`, fallback to `CurrentStock × CurrentCostPrice`
- NXT — movements from `StockTransaction` in date range, valued with snapshot/current cost fallback
- Near-expiry lots — `StockLot` / `LotStock` where `ExpiryDate` within threshold
- Cost history — `ProductCostHistory` rows

API prefix: `/api/reports/*`

---

## Analytics (Read-Only)

## Processing Flow

### Transfer (in-transit)

```text
Create Transfer (Draft) — transfer policy validated
        ↓
Approve (ship)
        ↓
TRANSFER_OUT @ source, TRANSFER_IN @ in-transit warehouse
        ↓
POST receive-transfer
        ↓
TRANSFER_OUT @ in-transit, TRANSFER_IN @ destination → Completed
```

### Standard inventory document

```text
Create document (Draft)
        ↓
Optional: Update draft (lines, reference, note)
        ↓
Approve
        ↓
StockLedgerService → StockTransaction(s) → CurrentStock
```

### Procurement receipt

```text
Create PO (Draft) → Submit
        ↓
Create GR (Draft) → Approve
        ↓
Stock In (auto) → StockTransaction → CurrentStock
        ↓
PO received qty updated
```

### POS sale

```text
confirm-sale
        ↓
Stock Out (Draft) → Approve (same request)
        ↓
StockTransaction (OUT) → CurrentStock decreased
```

---

## Design Principles

1. Every inventory movement creates a `StockTransaction`.
2. `CurrentStock` is never updated without a transaction.
3. Draft and cancelled documents do not affect stock.
4. Inventory cannot become negative.
5. Cost price is not assumed to be manually entered — domain supports external sources via `CostSource`.
6. Business documents must be traceable (document no, transaction no, audit log).
7. Pricing is a dedicated domain stream: regular price, promotion price, markdown history, cost history, valuation snapshots, and margin read models must stay traceable by effective date.
