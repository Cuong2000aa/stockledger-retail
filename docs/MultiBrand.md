# Multi-Brand & Omni-Channel Fulfillment

This document describes the **multi-brand** capability added to StockLedger Retail: brand-scoped master data, transfer policy, in-transit stock, brand-aware fulfillment, insights, and optional API scope headers.

---

## Why Multi-Brand?

Retail groups often operate several brands (or shops) under one platform. Requirements:

- Same SKU code may exist in different brands (scoped uniqueness).
- Warehouses belong to a brand (or shared DC with `BrandId = null`).
- Transfers across brands need explicit policy.
- Omni-channel allocation must not mix stock across brands.
- Transfer documents should support **ship → receive** (in-transit), not instant two-leg posting.

---

## Phase Overview

| Phase | Scope | Status |
|-------|--------|--------|
| **MB-1** | `Brand` entity, `BrandId` on Product / SKU / Warehouse, SKU unique per brand, Brand CRUD API, transfer policy validation, fulfillment brand filter | ✅ Done |
| **MB-2** | In-transit warehouse per brand, transfer approve = ship, `receive-transfer` = receive at destination | ✅ Done |
| **MB-3** | Insights (dead stock, velocity, transfer suggestions) filter by `brandId` / `regionCode`; suggestions match same brand + region | ✅ Done |
| **MB-4** | RBAC-lite via headers `X-Brand-Id`, `X-Warehouse-Ids`, `X-Region-Code` | ✅ Done |

Migration: `20260623060951_AddMultiBrandPhases`

---

## Core Entities

### Brand

| Field | Description |
|-------|-------------|
| `Code` | Unique brand code (e.g. `NIKE`, `ADIDAS`) |
| `Name` | Display name |
| `Status` | `Active` / `Inactive` |

**API:** `GET/POST /api/brands`, `GET/PUT /api/brands/{id}`

### TransferPolicy

Rules for **cross-brand** transfers. `SourceBrandId` / `DestinationBrandId` nullable = wildcard.

| Field | Description |
|-------|-------------|
| `AllowCrossBrand` | Must be `true` for cross-brand moves |
| `IsActive` | Policy enabled |

Same-brand transfers are always allowed (subject to stock rules). Cross-brand requires a matching active policy.

### Brand scope on master data

| Entity | Field | Notes |
|--------|-------|-------|
| `Product` | `BrandId` | Optional FK to `Brand` (legacy `Brand` text field kept) |
| `ProductVariant` | `BrandId` | Optional; inherits from product if null |
| `Warehouse` | `BrandId` | `null` = shared (e.g. group DC) |
| `Warehouse` | `RegionCode` | e.g. `HCM`, `HN` — replenishment & allocate |
| `Warehouse` | `FulfillmentPriority` | Lower number = higher priority when allocating |

**SKU uniqueness:** unique index on `(BrandId, Sku)` — same SKU string allowed for different brands.

### In-transit warehouse

- `WarehouseType.InTransit` (value `6`)
- Auto-created per brand: `IN_TRANSIT_{BRAND_CODE}` or `IN_TRANSIT_SHARED` when `BrandId` is null
- Not used as manual transfer endpoint

---

## Transfer Lifecycle (MB-2)

Previously, **approve** on a transfer posted OUT (source) + IN (destination) immediately.

**New flow:**

```text
Draft
  → Approve (ship)
      TRANSFER_OUT @ source
      TRANSFER_IN  @ in-transit warehouse
      Status: Approved, TransferLifecycleStatus: Shipped
  → POST receive-transfer
      TRANSFER_OUT @ in-transit
      TRANSFER_IN  @ destination
      Status: Completed, TransferLifecycleStatus: Received
```

| API | Effect |
|-----|--------|
| `POST /api/inventory-documents/transfer` | Create Draft; validates transfer policy |
| `POST /api/inventory-documents/{id}/approve` | Ship (source → in-transit) |
| `POST /api/inventory-documents/{id}/receive-transfer` | Receive (in-transit → destination) |

Document fields: `InTransitWarehouseId`, `TransferLifecycleStatus`, `ShippedAt`, `ReceivedAt`.

---

## Omni-Channel Fulfillment (brand-aware)

Existing multi-warehouse ATP APIs now accept optional scope:

| Parameter | Location | Purpose |
|-----------|----------|---------|
| `brandId` | Request body / query | SKU lookup `(BrandId, Sku)`; filter warehouses |
| `regionCode` | Request body / query | Prefer warehouses in region |

**APIs:**

- `POST /api/integration/fulfillment/check-availability-multi-warehouse`
- `POST /api/integration/fulfillment/allocate-warehouse`

Warehouse filter rules:

- If `brandId` set: include warehouses where `BrandId` is null (shared) or equals `brandId`
- If `regionCode` set: include warehouses where `RegionCode` is null or matches (case-insensitive)
- Order by `FulfillmentPriority`, then store/DC preference from config

---

## Inventory Insights (MB-3)

Query parameters on insight APIs:

| Parameter | APIs |
|-----------|------|
| `brandId` | dead-stock, sales-velocity, transfer-suggestions |
| `regionCode` | same |

Transfer suggestions only pair source/destination with **same brand** and **same region** (when region is set on both warehouses).

---

## API Scope Headers (MB-4)

Optional headers applied by `BrandScopeMiddleware` (RBAC-lite, no JWT yet):

| Header | Format | Effect |
|--------|--------|--------|
| `X-Brand-Id` | GUID | Default brand scope for fulfillment & insights |
| `X-Warehouse-Ids` | Comma-separated GUIDs | Restrict warehouse candidates |
| `X-Region-Code` | String | Default region filter |

Headers combine with explicit query/body parameters; header scope applies when parameter is omitted.

---

## Example Setup

```http
### 1. Create brand
POST /api/brands
{ "code": "BRAND_A", "name": "Brand A" }

### 2. Create warehouse with brand + region
POST /api/warehouses
{
  "code": "STORE_HCM_01",
  "name": "Store HCM 1",
  "type": 1,
  "brandId": "<brand-guid>",
  "regionCode": "HCM",
  "fulfillmentPriority": 10,
  "status": 1
}

### 3. Create product / SKU with same brandId
POST /api/products
{ "productCode": "P001", "name": "Polo", "brandId": "<brand-guid>" }

### 4. Transfer with scope header
X-Brand-Id: <brand-guid>
POST /api/inventory-documents/transfer
{ "sourceWarehouseId": "...", "destinationWarehouseId": "...", "lines": [...] }
```

---

## Cross-Brand Transfer Policy

Insert into `transfer_policies` (or future admin API):

```sql
INSERT INTO transfer_policies (id, "SourceBrandId", "DestinationBrandId", "AllowCrossBrand", "IsActive", "Note")
VALUES (gen_random_uuid(), '<source-brand>', '<dest-brand>', true, true, 'Allow replenishment between brands');
```

Without a matching policy, transfer create/approve fails with: *Cross-brand transfer is not allowed by transfer policy.*

---

## Related Files

| Area | Path |
|------|------|
| Brand API | `src/StockLedgerRetail.HttpApi/Controllers/BrandsController.cs` |
| Transfer policy | `src/StockLedgerRetail.Application/Inventory/TransferPolicyService.cs` |
| In-transit | `src/StockLedgerRetail.Application/Inventory/InTransitWarehouseService.cs` |
| Scope middleware | `host/StockLedgerRetail.HttpApi.Host/Middleware/BrandScopeMiddleware.cs` |
| Fulfillment | `src/StockLedgerRetail.Application/Integration/WarehouseFulfillmentService.cs` |
