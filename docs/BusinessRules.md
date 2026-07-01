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
| BR307     | One transfer ship/receive step creates two StockTransactions per line. |
| BR308     | Cross-brand transfer requires an active TransferPolicy with AllowCrossBrand. |
| BR309     | In-transit warehouses cannot be used as manual transfer source/destination. |
| BR310     | Approve on Transfer ships to in-transit; receive completes at destination. |
| BR311     | Product/SKU brand must be compatible with source and destination warehouse brand. |

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

---

# Draft Update Rules

| Rule Code | Rule |
| --------- | ---- |
| BR801 | Only Draft inventory documents can be updated. |
| BR802 | Approved or Cancelled documents are immutable. |
| BR803 | Update replaces line items as provided (full line set). |
| BR804 | Updating a draft must not create StockTransaction. |

---

# Purchase Order Rules

| Rule Code | Rule |
| --------- | ---- |
| BR901 | Supplier and destination warehouse are required. |
| BR902 | PO must have at least one line with OrderedQuantity > 0. |
| BR903 | Draft PO can be edited; Submitted PO cannot change lines. |
| BR904 | Submit transitions status Draft → Submitted. |
| BR905 | Cancel allowed only when no goods received (ReceivedQuantity = 0 on all lines). |
| BR906 | PO does not affect CurrentStock — stock changes via Goods Receipt only. |
| BR907 | ReceivedQuantity on PO line is updated when GR is approved. |
| BR908 | PO status becomes PartiallyReceived or Received based on line quantities. |
| BR909 | Users only see and act on PO/GR for warehouses they are assigned to (unless admin / all-warehouse scope). |
| BR910 | Partial receiving: multiple GRs per PO; total received must not exceed ordered quantity. |

---

# Goods Receipt Rules

| Rule Code | Rule |
| --------- | ---- |
| BR1001 | GR must reference a Submitted (or partially received) Purchase Order. |
| BR1002 | ReceivedQuantity per line must be > 0 and ≤ remaining on PO line. |
| BR1003 | Approve creates Stock In document with SourceSystem = PROCUREMENT. |
| BR1004 | Stock In from GR is auto-approved in the same operation. |
| BR1005 | GR stores link to created InventoryDocument (InventoryDocumentId). |
| BR1006 | Only Draft GR can be approved or cancelled. |

---

# POS Integration Rules

| Rule Code | Rule |
| --------- | ---- |
| BR1101 | check-availability is read-only — no StockTransaction created. |
| BR1102 | confirm-sale creates Stock Out and approves in one call. |
| BR1103 | confirm-return creates Stock In and approves in one call. |
| BR1104 | Idempotent by SourceSystem + ReferenceNo + DocumentType (unique index). |
| BR1105 | Duplicate confirm with same reference returns existing document, no double deduction. |
| BR1106 | Sale must pass available-stock validation (same as manual Stock Out). |

---

# Inventory Valuation Rules

| Rule Code | Rule |
| --------- | ---- |
| BR1201 | Legacy `CostPrice` and `SellingPrice` may remain nullable on SKU for backward compatibility. |
| BR1202 | `CurrentCostPrice`, `CurrentSellingPrice`, VAT-derived selling prices, and `CurrentCostSource` act as current operational cache on SKU. |
| BR1203 | CostSource indicates origin: Manual, Erp, Pos, PurchaseSystem. |
| BR1204 | ProductCostHistory records time-bounded cost (EffectiveFrom / EffectiveTo) and the current row is flagged by `IsCurrent`. |
| BR1205 | ProductCostHistory is readable via `GET /api/reports/cost-history`. |
| BR1206 | Negative cost or selling price is not allowed on SKU update. |
| BR1207 | VAT rate must be between 0 and 100. |
| BR1208 | ProductPrice stores effective-dated selling prices by `PriceType` (Regular, Promotion, Markdown, Clearance, Channel). |
| BR1209 | Updating a current Regular price refreshes the SKU current selling price cache. |
| BR1210 | Promotion and Markdown prices keep their own effective history and must not overwrite each other's current rows. |
| BR1211 | InventoryValuationSnapshot stores valuation by SKU / warehouse / snapshot date for reporting and analytics. |
| BR1212 | **Price before VAT** is the authoritative input when updating selling price; system derives price after VAT. |
| BR1213 | Backend rejects client payloads when after-VAT price does not match the rounding formula. |
| BR1214 | Margin on SKU DTOs is computed from cost and before-VAT selling price. |

---

# Multi-Brand Rules

| Rule Code | Rule |
| --------- | ---- |
| BR1301 | Brand `Code` must be unique. |
| BR1302 | SKU is unique per `(BrandId, Sku)` scope. |
| BR1303 | Warehouse with `BrandId` may only hold SKUs of that brand (or null-brand SKUs). |
| BR1304 | Fulfillment and insights respect optional `brandId` and `regionCode` filters. |
| BR1305 | Scope headers (`X-Brand-Id`, etc.) apply when request parameters are omitted. |
| BR1306 | Transfer suggestions pair warehouses with same brand and compatible region. |
| BR1307 | Each user may be assigned one or more warehouses; one **primary** warehouse for default filters. |
| BR1308 | Reports, current stock, PO, GR, and inventory documents respect the user's warehouse scope. |

---

# Approval Workflow Rules

| Rule Code | Rule |
| --------- | ---- |
| BR1401 | Document total value ≥ `DocumentValueThreshold` requires `submit-for-approval` before approve. |
| BR1402 | High-value documents may require multiple approval steps (`RequiredApprovalSteps`). |
| BR1403 | Stock is not posted until all required approval steps are completed. |
| BR1404 | Purchase orders above threshold enter `PendingApproval` on submit; `Approve` clears to `Submitted`. |

---

# Lot / Expiry Rules

| Rule Code | Rule |
| --------- | ---- |
| BR1501 | `TrackLotExpiry` on SKU enables lot master and lot stock balances. |
| BR1502 | Outbound deduction for lot-tracked SKUs follows FEFO (earliest expiry first). |
| BR1503 | Near-expiry report lists lots with `ExpiryDate` within configured days ahead. |

---

# Inventory Insights Rules

| Rule Code | Rule |
| --------- | ---- |
| BR1601 | Insights APIs are read-only; they never create documents or post `StockTransaction`. |
| BR1602 | Insight rows respect `warehouseId`, `brandId`, and `regionCode` filters; scope headers apply when query params are omitted. |
| BR1603 | Dead stock and markdown candidates require `QuantityOnHand ≥ minOnHand` and no outbound within `daysWithoutOutbound`. |
| BR1604 | Transfer suggestions pair source/destination warehouses with compatible brand and region; quantity derived from surplus vs target cover. |
| BR1605 | Pricing fields on insight DTOs come from SKU cache, `ProductPrice`, or `InventoryValuationSnapshot` — insights do not mutate prices. |
| BR1606 | Promotion-risk rows consider active or recent `ProductPrice` rows with `PriceType` Promotion or Markdown. |
| BR1607 | Reorder-risk combines low cover days with open purchase-order and goods-receipt pipeline quantities. |
| BR1608 | Trend summary compares current lookback window to the immediately prior period of equal length. |
| BR1609 | `InsightRecommendationEngine` may attach zero or more CTAs per row; actions deep-link to existing UI routes only. |
| BR1610 | Heavy insight queries may be served from `InsightSnapshot`; admin operations can trigger snapshot refresh. |

## Markdown policy (BR17xx)

| Rule Code | Rule |
| --------- | ---- |
| BR1701 | Each brand may have one or more active `MarkdownPolicy` rows; resolution prefers matching `RegionCode` and `WarehouseType` over brand-default. |
| BR1702 | Suggested markdown % must not exceed `MaxMarkdownPercent` and must respect `MinGrossMarginPercent` unless `AllowBelowCost` is true. |
| BR1703 | `MarkdownPolicyEngine` uses idle days, shop sell-through, and brand median sell-through — not idle days alone. |
| BR1704 | Insights compute suggestions only; persisting prices requires `ProductPrice` (`PriceType.Markdown`) via SKU UI or API. |
| BR1705 | When `RequireApprovalAbovePercent` is exceeded, DTO sets `MarkdownRequiresApproval` (operational hint; approval workflow optional). |
| BR1706 | System default tiers apply when no brand policy matches (10% / 15% / 25% by idle band). |

See [MarkdownPolicy.md](MarkdownPolicy.md).
