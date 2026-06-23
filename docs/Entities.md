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
* CostPrice
* SellingPrice
* CostSource
* CreatedAt
* UpdatedAt

### CostSource Values

* Manual
* Erp
* Pos
* PurchaseSystem

### Notes

Inventory is managed at ProductVariant level.

SKU is unique per `(BrandId, Sku)`.

Cost fields support future valuation analytics; they do not drive stock ledger logic.

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

## ProductCostHistory

Time-series cost records per SKU (domain prepared; no write API yet).

### Fields

* Id
* ProductVariantId
* CostPrice
* CostSource
* EffectiveFrom
* EffectiveTo (null = current record)

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
