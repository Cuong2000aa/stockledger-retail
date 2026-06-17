# Business Rules

---

# General Rules

| Rule Code | Rule                                                     |
| --------- | -------------------------------------------------------- |
| BR001     | Every inventory movement must create a StockTransaction. |
| BR002     | Only approved documents can affect inventory.            |
| BR003     | Draft documents must not affect inventory.               |
| BR004     | Cancelled documents must not affect inventory.           |
| BR005     | Inventory quantity cannot become negative.               |
| BR006     | ProductVariant must exist.                               |
| BR007     | Warehouse must exist.                                    |

---

# Stock In Rules

| Rule Code | Rule                                |
| --------- | ----------------------------------- |
| BR101     | Destination warehouse is required.  |
| BR102     | Quantity must be greater than zero. |
| BR103     | Create IN transaction.              |
| BR104     | Increase CurrentStock.              |

---

# Stock Out Rules

| Rule Code | Rule                                |
| --------- | ----------------------------------- |
| BR201     | Source warehouse is required.       |
| BR202     | Quantity must be greater than zero. |
| BR203     | Available stock must be sufficient. |
| BR204     | Create OUT transaction.             |
| BR205     | Decrease CurrentStock.              |

---

# Transfer Rules

| Rule Code | Rule                                                      |
| --------- | --------------------------------------------------------- |
| BR301     | Source warehouse is required.                             |
| BR302     | Destination warehouse is required.                        |
| BR303     | Source and destination warehouse cannot be the same.      |
| BR304     | Source warehouse must have enough available stock.        |
| BR305     | Create TRANSFER_OUT transaction.                          |
| BR306     | Create TRANSFER_IN transaction.                           |
| BR307     | One transfer operation must create two StockTransactions. |

---

# Adjustment Rules

| Rule Code | Rule                                         |
| --------- | -------------------------------------------- |
| BR401     | Adjustment reason is required.               |
| BR402     | Positive adjustment creates ADJUSTMENT_IN.   |
| BR403     | Negative adjustment creates ADJUSTMENT_OUT.  |
| BR404     | Adjustment cannot create negative inventory. |

---

# Stock Count Rules

| Rule Code | Rule                                                             |
| --------- | ---------------------------------------------------------------- |
| BR501     | Counted quantity cannot be negative.                             |
| BR502     | Variance = Counted Quantity - System Quantity.                   |
| BR503     | Positive variance creates COUNT_ADJUSTMENT_IN.                   |
| BR504     | Negative variance creates COUNT_ADJUSTMENT_OUT.                  |
| BR505     | No StockTransaction should be created when variance equals zero. |

---

# Current Stock Rules

| Rule Code | Rule                                                                      |
| --------- | ------------------------------------------------------------------------- |
| BR601     | CurrentStock must be updated after StockTransaction creation.             |
| BR602     | QuantityAvailable = QuantityOnHand - QuantityReserved.                    |
| BR603     | Only one CurrentStock record is allowed per ProductVariant and Warehouse. |
| BR604     | CurrentStock should store LastTransactionId.                              |

---

# Audit Rules

| Rule Code | Rule                                          |
| --------- | --------------------------------------------- |
| BR701     | Important user actions must be logged.        |
| BR702     | Document approval actions must be logged.     |
| BR703     | Document cancellation actions must be logged. |
| BR704     | Data changes should store old and new values. |

---

# Inventory Processing Flow

```text
InventoryDocument
        ↓
InventoryDocumentLine
        ↓
StockTransaction
        ↓
CurrentStock
```

---

# Golden Rules

## GR001

Never update CurrentStock directly without creating StockTransaction.

## GR002

StockTransaction is the source of truth for inventory auditing.

## GR003

CurrentStock is the source of truth for inventory lookup.

## GR004

All inventory movements must be traceable.

## GR005

Every inventory transaction must be linked to a business document.
