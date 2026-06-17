# Use Cases

## UC001 - Stock In

### Description

Receive inventory into a warehouse.

### Flow

1. User creates a STOCK_IN document.
2. User adds product variants and quantities.
3. User submits the document.
4. System validates product, warehouse and quantity.
5. System creates StockTransaction with positive QuantityDelta.
6. System updates CurrentStock.

### Business Rules

* Quantity must be greater than 0.
* DestinationWarehouseId is required.
* SourceWarehouseId is not required.
* ProductVariant must exist.
* Warehouse must exist.
* StockTransaction must be created after approval.

---

## UC002 - Stock Out

### Description

Issue inventory out of a warehouse.

### Flow

1. User creates a STOCK_OUT document.
2. User adds product variants and quantities.
3. User submits the document.
4. System validates available stock.
5. System creates StockTransaction with negative QuantityDelta.
6. System updates CurrentStock.

### Business Rules

* Quantity must be greater than 0.
* SourceWarehouseId is required.
* DestinationWarehouseId is not required.
* ProductVariant must exist.
* Warehouse must exist.
* Inventory cannot become negative.
* QuantityAvailable must be greater than or equal to requested quantity.

---

## UC003 - Inventory Adjustment

### Description

Adjust inventory quantity manually.

### Flow

1. User creates an ADJUSTMENT document.
2. User selects warehouse and product variants.
3. User enters adjustment quantity.
4. System validates current stock.
5. System creates StockTransaction.
6. System updates CurrentStock.

### Business Rules

* Adjustment can increase or decrease inventory.
* Adjustment must have a reason.
* Inventory cannot become negative after adjustment.
* Positive adjustment creates ADJUSTMENT_IN.
* Negative adjustment creates ADJUSTMENT_OUT.

---

## UC004 - Warehouse Transfer

### Description

Transfer inventory from one warehouse to another.

### Flow

1. User creates a TRANSFER document.
2. User selects source warehouse.
3. User selects destination warehouse.
4. User adds product variants and quantities.
5. System validates available stock in source warehouse.
6. System creates TRANSFER_OUT transaction for source warehouse.
7. System creates TRANSFER_IN transaction for destination warehouse.
8. System updates CurrentStock for both warehouses.

### Business Rules

* SourceWarehouseId is required.
* DestinationWarehouseId is required.
* SourceWarehouseId cannot be the same as DestinationWarehouseId.
* Quantity must be greater than 0.
* Source warehouse must have enough QuantityAvailable.
* Transfer must create two StockTransactions:

  * TRANSFER_OUT
  * TRANSFER_IN

---

## UC005 - Stock Count

### Description

Compare system inventory with physical counted inventory.

### Flow

1. User creates a STOCK_COUNT document.
2. User selects warehouse.
3. User enters counted quantity for product variants.
4. System compares counted quantity with current stock.
5. System calculates variance.
6. System creates COUNT_ADJUSTMENT transaction if variance exists.
7. System updates CurrentStock.

### Business Rules

* Counted quantity cannot be negative.
* Warehouse is required.
* If counted quantity is greater than current stock, create COUNT_ADJUSTMENT_IN.
* If counted quantity is less than current stock, create COUNT_ADJUSTMENT_OUT.
* If counted quantity equals current stock, no StockTransaction is needed.

---

# Common Rules

## CR001

Every inventory movement must create a StockTransaction.

## CR002

CurrentStock must always match the latest approved StockTransaction result.

## CR003

Inventory cannot become negative.

## CR004

DocumentNo must be unique.

## CR005

TransactionNo must be unique.

## CR006

Cancelled documents must not affect CurrentStock.

## CR007

Only approved or completed documents can affect inventory.

## CR008

Every document must have at least one line.
