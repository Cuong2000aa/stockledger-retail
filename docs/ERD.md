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
* created_by
* created_at
* approved_by
* approved_at

### Unique Indexes

* document_no

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
