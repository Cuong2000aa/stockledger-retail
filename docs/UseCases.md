# Use Cases

Retail inventory use cases for StockLedger Retail.  
**Status:** Phase 1–3 implemented in backend + frontend unless noted.

---

# UC001 — Stock In

**Actor:** Warehouse Staff

**Goal:** Increase inventory when goods arrive.

**Flow:**

1. Create Stock In document (Draft)
2. Add SKU lines with quantity
3. Approve document
4. System creates IN transactions and updates CurrentStock

**API:** `POST /api/inventory-documents/stock-in`, `POST .../{id}/approve`

**Status:** ✅ Implemented

---

# UC002 — Stock Out

**Actor:** Warehouse Staff / POS

**Goal:** Decrease inventory when goods leave the warehouse.

**Flow:**

1. Create Stock Out document (Draft)
2. Add SKU lines
3. Approve document
4. System validates available stock, creates OUT transactions, updates CurrentStock

**API:** `POST /api/inventory-documents/stock-out`, `POST .../{id}/approve`

**Status:** ✅ Implemented

---

# UC003 — Transfer Between Warehouses (In-Transit)

**Actor:** Warehouse Staff

**Goal:** Move stock from one warehouse to another via an in-transit leg.

**Flow:**

1. Create Transfer document (Draft) with source and destination warehouse — transfer policy validated
2. Add SKU lines
3. Approve document (**ship**) — TRANSFER_OUT at source, TRANSFER_IN at in-transit warehouse
4. Call `receive-transfer` — TRANSFER_OUT at in-transit, TRANSFER_IN at destination; status `Completed`

**API:** `POST /api/inventory-documents/transfer`, `POST .../{id}/approve`, `POST .../{id}/receive-transfer`

**Status:** ✅ Implemented (multi-brand + in-transit)

---

# UC004 — Stock Adjustment

**Actor:** Warehouse Manager

**Goal:** Correct inventory discrepancies (damage, found stock, data errors).

**Flow:**

1. Create Adjustment document (Draft) with reason
2. Add lines with signed quantity (+ increase, - decrease)
3. Approve document
4. System creates ADJUSTMENT_IN or ADJUSTMENT_OUT per line

**API:** `POST /api/inventory-documents/adjustment`, `POST .../{id}/approve`

**Status:** ✅ Implemented

---

# UC005 — Stock Count

**Actor:** Warehouse Staff

**Goal:** Reconcile system quantity with physical count.

**Flow:**

1. Create Stock Count document (Draft)
2. Enter counted quantity per SKU (system quantity shown for reference)
3. Approve document
4. For each line with variance ≠ 0: create COUNT_ADJUSTMENT_IN or COUNT_ADJUSTMENT_OUT

**API:** `POST /api/inventory-documents/stock-count`, `POST .../{id}/approve`

**Status:** ✅ Implemented

---

# UC006 — Update Draft Inventory Document

**Actor:** Warehouse Staff

**Goal:** Edit a draft document before approval (lines, reference, note).

**Flow:**

1. Open draft document
2. Update header fields and/or line items
3. Save — document remains Draft

**API:** `PUT /api/inventory-documents/{id}`

**Rules:** Only `Draft` documents can be updated. Approved or cancelled documents are immutable.

**Status:** ✅ Implemented

---

# UC007 — Purchase Order

**Actor:** Procurement Staff

**Goal:** Order goods from a supplier.

**Flow:**

1. Create Purchase Order (Draft) — select supplier, warehouse, lines
2. Submit PO → status `Submitted`
3. Optionally cancel if no goods received yet

**API:** `POST /api/purchase-orders`, `POST .../{id}/submit`, `POST .../{id}/cancel`

**Note:** PO does not change stock. Stock changes only via Goods Receipt.

**Status:** ✅ Implemented

---

# UC008 — Goods Receipt (Procurement)

**Actor:** Warehouse Staff

**Goal:** Receive goods against a submitted purchase order.

**Flow:**

1. Create Goods Receipt (Draft) linked to PO — enter received quantities per line
2. Approve GR
3. System auto-creates and approves Stock In document (`SourceSystem = PROCUREMENT`)
4. PO line `ReceivedQuantity` updated; PO status → `PartiallyReceived` or `Received`

**API:** `POST /api/goods-receipts`, `POST .../{id}/approve`

**Status:** ✅ Implemented

---

# UC009 — POS Sales Integration

**Actor:** POS / OMS / E-commerce

**Goal:** Check stock and confirm sales/returns without managing inventory locally.

**Flows:**

**Check availability (read-only):**

1. POS sends SKU + warehouse + quantity
2. System returns available quantity

**Confirm sale:**

1. POS sends order reference + lines
2. System creates Stock Out, approves, deducts stock
3. Idempotent: duplicate `sourceSystem + orderReference` does not deduct twice

**Confirm return:**

1. POS sends return reference + lines
2. System creates Stock In, approves, increases stock
3. Idempotent by `sourceSystem + returnReference`

**API:** `POST /api/integration/sales/check-availability`, `confirm-sale`, `confirm-return`

**Status:** ✅ Implemented

---

# UC010 — Inventory Analytics

**Actor:** Manager / Dashboard

**Goal:** View stock and movement summaries without changing data.

**Queries:**

- Summary totals (SKUs, warehouses, open POs, pending GRs)
- Stock by warehouse
- In/out movements over date range
- Low-stock SKUs below threshold

**API:** `GET /api/analytics/summary`, `stock-by-warehouse`, `movements`, `low-stock`

**Status:** ✅ Implemented (basic analytics)

---

# UC011 — Inventory Valuation (SKU Cost)

**Actor:** Merchandising / Finance

**Goal:** Maintain enterprise pricing and valuation data for current operations, reporting, and future AI/analytics features.

**Current scope:**

1. Maintain SKU current cost cache and VAT-aware current selling cache on `ProductVariant`
2. Maintain effective-dated `ProductPrice` rows for `Regular`, `Promotion`, and `Markdown`
3. Maintain `ProductCostHistory` for effective-dated cost changes
4. Maintain `InventoryValuationSnapshot` for reporting and analytics

**API:** `PUT /api/product-variants/{id}`, `GET/POST /api/product-variants/{id}/prices`

**Status:** ✅ Current cache + ProductPrice history + ProductCostHistory + valuation snapshot implemented

---

# UC012 — Inventory Insights

**Actor:** Manager / AI Copilot

**Goal:** Decision support — dead stock, sales velocity, transfer suggestions.

**API:** `GET /api/inventory-insights/dead-stock`, `sales-velocity`, `transfer-suggestions` (optional `brandId`, `regionCode`)

**Status:** ✅ Implemented (rule-based, snapshot cache, recommendation cards)

---

# UC013 — Brand Master Data

**Actor:** Admin / Merchandising

**Goal:** Define brands and scope products, SKUs, and warehouses.

**API:** `GET/POST /api/brands`, `GET/PUT /api/brands/{id}`

**Status:** ✅ Implemented (backend API + admin list UI)

---

# UC014 — Cross-Brand Transfer Policy

**Actor:** Operations / Admin

**Goal:** Allow or deny transfers between warehouses of different brands.

**Flow:** `TransferPolicy` rows; validated on transfer create/approve; managed via admin API.

**API:** `GET/POST/PUT /api/admin/transfer-policies`

**Status:** ✅ Implemented

---

# UC015 — Omni-Channel Warehouse Allocation

**Actor:** OMS / Marketplace integration

**Goal:** Select best warehouse to fulfill an order with brand and region scope.

**API:** `POST /api/integration/fulfillment/check-availability-multi-warehouse`, `allocate-warehouse`

**Status:** ✅ Implemented

---

# UC016 — API Scope Headers

**Actor:** Integration / Portal

**Goal:** Restrict fulfillment and insights to a brand, warehouse list, or region via headers.

**Headers:** `X-Brand-Id`, `X-Warehouse-Ids`, `X-Region-Code`

**Status:** ✅ Implemented (RBAC-lite)

---

# UC017 — Inventory Reports

**Actor:** Manager / Finance / Warehouse

**Goal:** Read-only valuation and movement reports without changing stock.

**Queries:**

- Inventory value by SKU/warehouse (`CostPrice × QuantityOnHand`)
- NXT (opening / in / out / closing) for a date range
- Near-expiry lots and lot stock balances
- SKU cost history

**API:** `GET /api/reports/inventory-value`, `nxt`, `near-expiry-lots`, `lot-stocks`, `cost-history`

**Status:** ✅ Implemented

---

# UC018 — Stock Reservation Admin

**Actor:** Warehouse / System admin

**Goal:** View and release POS/OMS stock holds when orders expire or are cancelled.

**API:** `GET /api/stock-reservations`, `POST /api/stock-reservations/{id}/release`

**Status:** ✅ Implemented (list + release; reservations created by integration APIs)

---

# UC019 — Multi-Step Document Approval

**Actor:** Warehouse clerk / Team leader

**Goal:** High-value inventory documents require explicit submission and two approval steps before posting.

**Flow:**

1. Create document (Draft)
2. `POST .../submit-for-approval` when total line value ≥ threshold
3. Approver(s) call `POST .../approve` — may require 2 steps (`RequiredApprovalSteps`)
4. Stock posts only after all steps complete

**Config:** `ApprovalWorkflow:DocumentValueThreshold` (default 10,000,000 VND)

**Status:** ✅ Implemented (inventory documents + PO `PendingApproval`)

---

# UC020 — Lot / Expiry Tracking (FEFO)

**Actor:** Warehouse Staff

**Goal:** Track batch/lot and expiry for SKUs that require it (`TrackLotExpiry` on SKU).

**Domain:** `StockLot` (lot code, expiry), `LotStock` (quantity per warehouse), `LotStockService` for FEFO allocation.

**Reports:** `GET /api/reports/near-expiry-lots`, `lot-stocks`

**Status:** ✅ Implemented (domain + reports; outbound FEFO on stock-out integration path)

---

# UC021 — Background Operations Dashboard

**Actor:** System admin (`system.admin`)

**Goal:** Monitor and trigger background jobs (stock reconciliation, insight snapshot refresh).

**API:** `GET /api/admin/operations`, `PUT/POST .../jobs/{jobKey}`

**Status:** ✅ Implemented

---

# Document Number Prefixes

| Type | Prefix |
|------|--------|
| Stock In | SI |
| Stock Out | SO |
| Transfer | TR |
| Adjustment | AD |
| Stock Count | SC |
| Purchase Order | PO |
| Goods Receipt | GR |
