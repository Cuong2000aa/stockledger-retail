# Entity Relationship Design

## Overview

This document defines the initial database relationship design for StockLedger Retail.

The database is designed around inventory traceability, stock accuracy, and business document control.

---

# Tables

## products

Product master table.

### Primary Key

* id

### Columns

* id
* product_code
* name
* brand
* category
* status
* created_at
* updated_at

### Unique Indexes

* product_code

---

## product_variants

Sellable SKU table.

### Primary Key

* id

### Foreign Keys

* product_id references products(id)

### Columns

* id
* product_id
* sku
* barcode
* color
* size
* season
* unit
* status
* cost_price
* selling_price
* cost_source
* created_at
* updated_at

### Unique Indexes

* sku
* barcode

---

## warehouses

Warehouse, store, and sub warehouse master table.

### Primary Key

* id

### Foreign Keys

* parent_warehouse_id references warehouses(id)

### Columns

* id
* code
* name
* type
* parent_warehouse_id
* status
* created_at
* updated_at

### Unique Indexes

* code

---

## current_stocks

Current inventory quantity by product variant and warehouse.

### Primary Key

* id

### Foreign Keys

* product_variant_id references product_variants(id)
* warehouse_id references warehouses(id)
* last_transaction_id references stock_transactions(id)

### Columns

* id
* product_variant_id
* warehouse_id
* quantity_on_hand
* quantity_reserved
* quantity_available
* last_transaction_id
* last_updated_at

### Unique Indexes

* product_variant_id
* warehouse_id

### Notes

quantity_available = quantity_on_hand - quantity_reserved

---

## inventory_documents

Inventory business document header.

### Primary Key

* id

### Foreign Keys

* source_warehouse_id references warehouses(id)
* destination_warehouse_id references warehouses(id)

### Columns

* id
* document_no
* document_type
* source_warehouse_id
* destination_warehouse_id
* status
* document_date
* reference_no
* note
* source_system
* created_by
* created_at
* approved_by
* approved_at

### Unique Indexes

* document_no

### Integration Index

* (source_system, reference_no, document_type) — unique when both source_system and reference_no are set (idempotent POS / integration)

---

## inventory_document_lines

Inventory business document line.

### Primary Key

* id

### Foreign Keys

* document_id references inventory_documents(id)
* product_variant_id references product_variants(id)

### Columns

* id
* document_id
* product_variant_id
* quantity
* unit_cost
* note

### Indexes

* document_id
* product_variant_id

---

## stock_transactions

Inventory ledger table.

### Primary Key

* id

### Foreign Keys

* document_id references inventory_documents(id)
* document_line_id references inventory_document_lines(id)
* product_variant_id references product_variants(id)
* warehouse_id references warehouses(id)

### Columns

* id
* transaction_no
* document_id
* document_line_id
* product_variant_id
* warehouse_id
* transaction_type
* quantity_delta
* before_quantity
* after_quantity
* transaction_date
* created_by
* created_at

### Unique Indexes

* transaction_no

### Indexes

* product_variant_id
* warehouse_id
* transaction_date
* document_id
* document_line_id

---

## transaction_logs

Technical audit log.

### Primary Key

* id

### Columns

* id
* entity_name
* entity_id
* action
* old_value
* new_value
* created_by
* created_at
* ip_address

### Indexes

* entity_name
* entity_id
* created_at

---

## suppliers

Supplier master for procurement.

### Primary Key

* id

### Columns

* id
* code
* name
* contact_name
* phone
* email
* address
* status
* created_at
* updated_at

### Unique Indexes

* code

---

## purchase_orders

Purchase order header.

### Primary Key

* id

### Foreign Keys

* supplier_id references suppliers(id)
* warehouse_id references warehouses(id)

### Columns

* id
* po_no
* supplier_id
* warehouse_id
* status
* order_date
* expected_date
* reference_no
* note
* created_by
* created_at
* submitted_at
* cancelled_at

### Unique Indexes

* po_no

---

## purchase_order_lines

Purchase order line items.

### Primary Key

* id

### Foreign Keys

* purchase_order_id references purchase_orders(id)
* product_variant_id references product_variants(id)

### Columns

* id
* purchase_order_id
* product_variant_id
* ordered_quantity
* received_quantity
* unit_cost
* note

### Indexes

* purchase_order_id
* product_variant_id

---

## goods_receipts

Goods receipt header (procurement receiving).

### Primary Key

* id

### Foreign Keys

* purchase_order_id references purchase_orders(id)
* warehouse_id references warehouses(id)
* inventory_document_id references inventory_documents(id) (nullable until approved)

### Columns

* id
* gr_no
* purchase_order_id
* warehouse_id
* status
* receipt_date
* reference_no
* note
* inventory_document_id
* created_by
* created_at
* approved_by
* approved_at

### Unique Indexes

* gr_no

---

## goods_receipt_lines

Goods receipt line items.

### Primary Key

* id

### Foreign Keys

* goods_receipt_id references goods_receipts(id)
* purchase_order_line_id references purchase_order_lines(id)
* product_variant_id references product_variants(id)

### Columns

* id
* goods_receipt_id
* purchase_order_line_id
* product_variant_id
* received_quantity
* unit_cost
* note

### Indexes

* goods_receipt_id
* purchase_order_line_id

---

## product_cost_histories

Time-series cost per SKU (valuation domain; no write API yet).

### Primary Key

* id

### Foreign Keys

* product_variant_id references product_variants(id)

### Columns

* id
* product_variant_id
* cost_price
* cost_source
* effective_from
* effective_to

### Indexes

* product_variant_id
* effective_from

---

# Relationships

## Product to ProductVariant

One Product has many ProductVariants.

```text
products 1 ---- * product_variants
```

---

## Warehouse Hierarchy

One Warehouse can have many child Warehouses.

```text
warehouses 1 ---- * warehouses
```

Example:

```text
STORE_Q1
├── SELLING_AREA
├── BACKROOM
├── DEFECT
└── RETURN
```

---

## ProductVariant to CurrentStock

One ProductVariant can have stock in many Warehouses.

```text
product_variants 1 ---- * current_stocks
warehouses        1 ---- * current_stocks
```

---

## InventoryDocument to InventoryDocumentLine

One InventoryDocument has many InventoryDocumentLines.

```text
inventory_documents 1 ---- * inventory_document_lines
```

---

## InventoryDocumentLine to StockTransaction

One InventoryDocumentLine can generate one or many StockTransactions.

Examples:

* STOCK_IN creates one IN transaction.
* STOCK_OUT creates one OUT transaction.
* TRANSFER creates two transactions:

  * TRANSFER_OUT
  * TRANSFER_IN

```text
inventory_document_lines 1 ---- * stock_transactions
```

---

## Supplier to PurchaseOrder

```text
suppliers 1 ---- * purchase_orders
warehouses 1 ---- * purchase_orders
```

---

## PurchaseOrder to PurchaseOrderLine

```text
purchase_orders 1 ---- * purchase_order_lines
product_variants 1 ---- * purchase_order_lines
```

---

## PurchaseOrder to GoodsReceipt

```text
purchase_orders 1 ---- * goods_receipts
```

---

## GoodsReceipt to GoodsReceiptLine

```text
goods_receipts 1 ---- * goods_receipt_lines
purchase_order_lines 1 ---- * goods_receipt_lines
```

---

## GoodsReceipt to InventoryDocument

On approve, one GoodsReceipt links to one auto-created Stock In document.

```text
goods_receipts 1 ---- 0..1 inventory_documents
```

---

## ProductVariant to ProductCostHistory

```text
product_variants 1 ---- * product_cost_histories
```

---

# Design Notes

## Stock Ledger Principle

Every inventory movement must be recorded in stock_transactions.

current_stocks is used for fast lookup.

stock_transactions is used for traceability and auditing.

---

## Transfer Design

A transfer document should create two stock transactions:

```text
Source Warehouse      TRANSFER_OUT    -Quantity
Destination Warehouse TRANSFER_IN     +Quantity
```

---

## Stock Count Design

A stock count document should only create transactions when there is variance.

Example:

```text
System Quantity = 100
Counted Quantity = 98
Variance = -2
Transaction = COUNT_ADJUSTMENT_OUT
```

---

## Negative Stock Rule

CurrentStock cannot become negative.

Before creating any OUT transaction, the system must validate available stock.

---

## Audit Rule

stock_transactions stores inventory movement history.

transaction_logs stores user or system action history.

---

## Procurement Flow

```text
suppliers
    ↓
purchase_orders → purchase_order_lines
    ↓
goods_receipts → goods_receipt_lines
    ↓ (approve)
inventory_documents (STOCK_IN, source_system = PROCUREMENT)
    ↓
stock_transactions → current_stocks
```

PO received_quantity is updated when GR is approved; PO status reflects partial or full receipt.

---

## Valuation Note

product_variants.cost_price / selling_price / cost_source are master fields on SKU.

product_cost_histories is prepared for time-bounded cost tracking; not yet wired to application services.
