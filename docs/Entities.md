# Entities Design

## Product

Represents a product master record.

### Fields

* Id
* ProductCode
* Name
* Brand (text, legacy)
* BrandId (FK → Brand, optional)
* Category
* Status
* CreatedAt
* UpdatedAt

---

## Brand

Multi-brand master.

### Fields

* Id
* Code (unique)
* Name
* Status (`Active`, `Inactive`)
* CreatedAt
* UpdatedAt

---

## TransferPolicy

Cross-brand transfer rules.

### Fields

* Id
* SourceBrandId (nullable)
* DestinationBrandId (nullable)
* AllowCrossBrand
* IsActive
* Note

---

## MarkdownPolicy

Per-brand markdown / clearance pricing rules (tiers stored as JSON).

### Fields

* Id
* BrandId
* RegionCode (optional override)
* WarehouseType (optional override)
* LookbackDays
* MinDaysWithoutOutbound
* MinOnHand
* MinInventoryValueAtCost (optional)
* MinGrossMarginPercent
* MaxMarkdownPercent
* AllowBelowCost
* RequireApprovalAbovePercent (optional)
* SlowSellThroughThreshold
* TiersJson (JSON array of tier objects)
* IsActive
* Note

See [MarkdownPolicy.md](MarkdownPolicy.md).

---

## ProductVariant

Represents a sellable SKU.

### Fields

* Id
* ProductId
* BrandId (optional)
* SKU
* Barcode
* Color
* Size
* Season
* Unit
* Status
* CostPrice (legacy compatibility)
* SellingPrice (legacy compatibility)
* CostSource (legacy compatibility)
* CurrentCostPrice
* CurrentSellingPrice
* CurrentSellingPriceBeforeVat
* CurrentSellingPriceAfterVat
* VatRate
* CurrentCostSource
* CurrentCostEffectiveFrom
* CurrentPriceEffectiveFrom
* TrackLotExpiry (bool — enable lot/HSD tracking)
* IsBarcode (bool — require per-unit barcode on inbound/outbound lines)
* CreatedAt
* UpdatedAt

### CostSource Values

* Manual
* Erp
* Pos
* PurchaseSystem

### Pricing Notes

Inventory is managed at ProductVariant level.

SKU is unique per `(BrandId, Sku)`.

ProductVariant now keeps current pricing/cost cache for operational reads, while historical pricing and valuation live in dedicated entities.

---

## ProductPrice

Effective-dated selling price records per SKU.

### Fields

* Id
* ProductVariantId
* PriceType
* PriceBeforeVat
* VatRate
* PriceAfterVat
* Currency
* EffectiveFrom
* EffectiveTo
* IsCurrent
* ChannelCode
* ReferenceType
* ReferenceId
* CreatedBy
* CreatedAt
* UpdatedBy
* UpdatedAt

### PriceType Values

* Regular
* Markdown
* Promotion
* Clearance
* Channel

---

## Product (notes)

A Product is a parent entity. Example: Polo Shirt with variants POLO-BLK-M, POLO-BLK-L.

---

## Warehouse

Represents a warehouse, store, or sub warehouse.

### Fields

* Id
* Code
* Name
* Type
* ParentWarehouseId
* BrandId (optional)
* RegionCode (optional)
* FulfillmentPriority
* AddressLine, Ward, District, Province, PostalCode, Phone, ContactName, FullAddress
* Status
* CreatedAt
* UpdatedAt

### Warehouse Types

* DC
* STORE
* SUB_WAREHOUSE
* DEFECT
* RETURN
* IN_TRANSIT

### Examples

DC_HCM

Store_Q1

Store_Q7

---

## CurrentStock

Represents current inventory quantity.

### Fields

* Id
* ProductVariantId
* WarehouseId
* QuantityOnHand
* QuantityReserved
* QuantityAvailable
* LastTransactionId
* LastUpdatedAt

### Formula

QuantityAvailable = QuantityOnHand - QuantityReserved

### Constraints

Unique:

* ProductVariantId
* WarehouseId

---

## InventoryDocument

Represents inventory business documents.

### Fields

* Id
* DocumentNo
* DocumentType
* SourceWarehouseId
* DestinationWarehouseId
* Status
* DocumentDate
* ReferenceNo
* Note
* SourceSystem
* CreatedBy
* CreatedAt
* ApprovedBy
* ApprovedAt
* TransferLifecycleStatus
* InTransitWarehouseId
* ShippedAt
* ReceivedAt

### Document Types

* STOCK_IN
* STOCK_OUT
* TRANSFER
* ADJUSTMENT
* STOCK_COUNT

### Status

* DRAFT
* PENDING
* APPROVED
* COMPLETED
* CANCELLED

**In use:** Draft → Approved (or Cancelled from Draft). Transfer: Approved (shipped) → Completed (received). Pending reserved for future workflow.

### SourceSystem

Optional origin identifier for integration idempotency (e.g. `POS`, `PROCUREMENT`).

---

## InventoryDocumentLine

Represents document line items.

### Fields

* Id
* DocumentId
* ProductVariantId
* Quantity
* UnitCost
* Note

---

## StockTransaction

Inventory ledger.

Every inventory movement must create a StockTransaction.

### Fields

* Id
* TransactionNo
* DocumentId
* DocumentLineId
* ProductVariantId
* WarehouseId
* TransactionType
* QuantityDelta
* BeforeQuantity
* AfterQuantity
* TransactionDate
* CreatedBy
* CreatedAt

### Transaction Types

* IN
* OUT
* TRANSFER_IN
* TRANSFER_OUT
* ADJUSTMENT_IN
* ADJUSTMENT_OUT
* COUNT_ADJUSTMENT_IN
* COUNT_ADJUSTMENT_OUT

### Examples

Receive 100 items

QuantityDelta = +100

Sell 5 items

QuantityDelta = -5

---

## TransactionLog

Technical audit log.

### Fields

* Id
* EntityName
* EntityId
* Action
* OldValue
* NewValue
* CreatedBy
* CreatedAt
* IpAddress

### Example Actions

* CREATE
* UPDATE
* DELETE
* APPROVE
* CANCEL

### Notes

TransactionLog is different from StockTransaction.

StockTransaction:

* Inventory movement history

TransactionLog:

* User activity history

---

## Supplier

Procurement vendor master data.

### Fields

* Id
* Code
* Name
* ContactName
* Phone
* Email
* Address
* Status (Active / Inactive)
* CreatedAt
* UpdatedAt

---

## PurchaseOrder

Order to supplier. Does not affect stock until goods are received.

### Fields

* Id
* PoNo
* SupplierId
* WarehouseId
* Status (Draft, Submitted, PartiallyReceived, Received, Cancelled)
* OrderDate
* ExpectedDate
* ReferenceNo
* Note
* CreatedBy
* CreatedAt
* SubmittedAt
* CancelledAt

### PurchaseOrderLine

* Id
* PurchaseOrderId
* ProductVariantId
* OrderedQuantity
* ReceivedQuantity
* UnitCost
* Note

---

## GoodsReceipt

Physical receipt against a purchase order.

### Fields

* Id
* GrNo
* PurchaseOrderId
* WarehouseId
* Status (Draft, Approved, Cancelled)
* ReceiptDate
* ReferenceNo
* Note
* InventoryDocumentId (set on approve — linked Stock In)
* CreatedBy
* CreatedAt
* ApprovedBy
* ApprovedAt

### GoodsReceiptLine

* Id
* GoodsReceiptId
* PurchaseOrderLineId
* ProductVariantId
* ReceivedQuantity
* UnitCost
* Note

---

---

## StockLot

Batch/lot master for a SKU.

### Fields

* Id
* ProductVariantId
* LotCode
* ExpiryDate (optional)
* ReceivedAt

---

## LotStock

On-hand quantity for a lot at a warehouse.

### Fields

* Id
* StockLotId
* WarehouseId
* QuantityOnHand
* LastUpdatedAt

---

## ProductCostHistory

Time-series cost records per SKU.

### Fields

* Id
* ProductVariantId
* CostPrice
* CostSource
* ValuationMethod
* Currency
* ReferenceType
* ReferenceId
* EffectiveFrom
* EffectiveTo (null = current record)
* IsCurrent

**API:** `GET /api/reports/cost-history`

---

## InventoryValuationSnapshot

Persisted valuation snapshot per SKU / warehouse / date.

### Fields

* Id
* ProductVariantId
* WarehouseId
* QuantityOnHand
* QuantityReserved
* QuantityAvailable
* AverageCost
* InventoryValue
* SnapshotDate
* ValuationMethod
* Currency
* CreatedBy
* CreatedAt
* UpdatedBy
* UpdatedAt

---

## VariantUnitBarcode

Per-unit barcode (IMEI/serial) when SKU `IsBarcode = true`.

### Fields

* Id
* ProductVariantId
* Barcode (unique)
* WarehouseId (optional — current location)
* Status (`InStock`, `Sold`, `Returned`, …)
* ReceivedAt
* LastUpdatedAt

**API:** `GET /api/unit-barcodes`

Line-level barcode snapshots: `InventoryDocumentLineBarcode`, `PurchaseOrderLineBarcode`, `GoodsReceiptLineBarcode`, `StockTransactionBarcode`.

---

## StockReservation

POS/OMS stock hold before confirm-sale.

### Fields

* Id, ReservationNo
* SourceSystem, ReferenceType, ReferenceKey
* WarehouseId, Status, ExpiresAt
* CommittedAt, ReleasedAt
* Lines: `ProductVariantId`, `Quantity`

**API:** `POST /api/integration/sales/reserve`, `release-reservation`; admin `GET /api/stock-reservations`

---

## AppUser & RBAC (summary)

* **AppUser** — Email, DisplayName, PasswordHash, IsActive
* **Permission**, **PermissionGroup**, **GroupPermission**
* **UserGroupAssignment**, **UserWarehouseAssignment**
* **Team**, **TeamMember**

See [RBAC.md](RBAC.md).

---

## InsightSnapshot & InsightActionLog

* **InsightSnapshot** — cached insight JSON (`SnapshotKey`, `InsightKind`, `PayloadJson`)
* **InsightActionLog** — CTA click audit (`InsightKind`, `ActionCode`, variant/warehouse IDs)

**API:** `POST /api/insight-actions`; job `insight_snapshots`

---

## InventoryDailyRollup

Daily inventory KPI rollup: `SnapshotDate`, `BrandId`, `WarehouseId`, `RegionCode`, `SkuCount`, `TotalOnHand`, `TotalInventoryValue`, `OutboundQty30d`.

Job: `inventory_daily_rollups`

---

## BackgroundJobSetting / BackgroundJobRun

Background job config and run history. Keys: `insight_snapshots`, `insight_alerts`, `stock_reconciliation`, `reservation_expiry`, `inventory_daily_rollups`.

**API:** `/api/admin/operations`

---

# Design Principles

## Rule 1

Every inventory movement must create a StockTransaction.

## Rule 2

CurrentStock is the source for fast inventory lookup.

## Rule 3

StockTransaction is the source for inventory auditing.

## Rule 4

Inventory quantity cannot become negative.

## Rule 5

All business documents must be traceable.

## Rule 6

Every document must contain at least one document line.
