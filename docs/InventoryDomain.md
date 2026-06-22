# Inventory Domain

Overview of the retail inventory domain as implemented in StockLedger Retail.

---

## Core Entities

### Product & ProductVariant (SKU)

- **Product** — parent master data (code, name, brand, category).
- **ProductVariant** — the actual inventory unit. All stock is tracked at SKU level.
- **Valuation fields** on SKU (optional, for future analytics):
  - `CostPrice` — cost price (may come from ERP, POS, Purchase System, or Manual entry)
  - `SellingPrice` — retail selling price
  - `CostSource` — `Manual`, `Erp`, `Pos`, `PurchaseSystem`
- **ProductCostHistory** — time-series cost records (`EffectiveFrom` / `EffectiveTo`); entity mapped, no API yet.

### Warehouse

Physical or logical storage location: DC, Store, Sub-warehouse, Defect, Return. Supports parent-child hierarchy.

### CurrentStock

Fast lookup of on-hand quantity per SKU per warehouse.

```
QuantityAvailable = QuantityOnHand - QuantityReserved
```

`QuantityReserved` is stored but not yet used by business logic (planned).

### InventoryDocument

Business document header. Types: `StockIn`, `StockOut`, `Transfer`, `Adjustment`, `StockCount`.

Statuses: `Draft` → `Approved` (or `Cancelled` while Draft).

Only **approved** documents generate `StockTransaction` records.

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

## POS Integration

External sales systems (POS, OMS) call integration APIs instead of managing stock directly.

- **Check availability** — read-only
- **Confirm sale** — Stock Out, auto-approved, idempotent by `sourceSystem + orderReference`
- **Confirm return** — Stock In, auto-approved, idempotent by `sourceSystem + returnReference`

---

## Analytics (Read-Only)

Aggregated views over `CurrentStock`, `StockTransaction`, and procurement data. No write operations.

Planned extension: **Inventory Insights** (dead stock, markdown simulation, transfer suggestions) as rule-based APIs without AI.

---

## Processing Flow

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
