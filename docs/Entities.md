# Entities Design

## Product

Represents a product master record.

### Fields

* Id
* ProductCode
* Name
* Brand
* Category
* Status
* CreatedAt
* UpdatedAt

### Notes

A Product is a parent entity.

Example:

Product: Polo Shirt

Variants:

* POLO-BLK-M
* POLO-BLK-L
* POLO-WHT-M

---

## ProductVariant

Represents a sellable SKU.

### Fields

* Id
* ProductId
* SKU
* Barcode
* Color
* Size
* Season
* Unit
* Status
* CreatedAt
* UpdatedAt

### Notes

Inventory is managed at ProductVariant level.

---

## Warehouse

Represents a warehouse, store, or sub warehouse.

### Fields

* Id
* Code
* Name
* Type
* ParentWarehouseId
* Status
* CreatedAt
* UpdatedAt

### Warehouse Types

* DC
* STORE
* SUB_WAREHOUSE
* DEFECT
* RETURN

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
* CreatedBy
* CreatedAt
* ApprovedBy
* ApprovedAt

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
