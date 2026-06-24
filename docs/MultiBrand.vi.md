# Đa thương hiệu (Multi-Brand) & Omni-Channel

Tài liệu mô tả năng lực **đa brand** trên StockLedger Retail: master data theo brand, chính sách chuyển kho, tồn đang luân chuyển, allocate omni-channel, insights và phạm vi API qua header.

---

## Vì sao cần Multi-Brand?

Nhóm bán lẻ thường vận hành nhiều thương hiệu / shop trên một nền tảng:

- Cùng mã SKU có thể tồn tại ở nhiều brand (unique theo `(BrandId, Sku)`).
- Kho gắn brand (hoặc DC dùng chung khi `BrandId = null`).
- Chuyển kho khác brand cần `TransferPolicy`.
- Allocate omni-channel không được trộn tồn giữa các brand.
- Phiếu chuyển kho: **xuất (ship) → nhận (receive)** qua kho in-transit.

---

## Các phase đã triển khai

| Phase | Nội dung | Trạng thái |
|-------|----------|------------|
| **MB-1** | Entity `Brand`, `BrandId` trên Product/SKU/Warehouse, API brand, validate transfer policy, lọc fulfillment theo brand | ✅ |
| **MB-2** | Kho in-transit theo brand, approve = ship, `receive-transfer` = nhận tại kho đích | ✅ |
| **MB-3** | Insights lọc `brandId` / `regionCode`; gợi ý chuyển kho cùng brand + vùng | ✅ |
| **MB-4** | Header `X-Brand-Id`, `X-Warehouse-Ids`, `X-Region-Code` | ✅ |

Migration: `20260623060951_AddMultiBrandPhases`

---

## Entity chính

### Brand

- `Code` — mã duy nhất
- `Name`, `Status` (`Active` / `Inactive`)
- API: `GET/POST /api/brands`, `GET/PUT /api/brands/{id}`

### TransferPolicy

Quy tắc chuyển **khác brand**. `SourceBrandId` / `DestinationBrandId` null = áp dụng mọi brand.

- Cùng brand: luôn cho phép (nếu đủ tồn).
- Khác brand: cần policy `AllowCrossBrand = true` và đang `IsActive`.

### Gắn brand trên master data

| Entity | Trường | Ghi chú |
|--------|--------|---------|
| Product | `BrandId` | FK tùy chọn (giữ trường text `Brand` cũ) |
| ProductVariant | `BrandId` | Kế thừa từ product nếu null |
| Warehouse | `BrandId` | null = kho dùng chung |
| Warehouse | `RegionCode` | VD: `HCM`, `HN` |
| Warehouse | `FulfillmentPriority` | Số nhỏ = ưu tiên cao khi allocate |

**SKU:** unique theo `(BrandId, Sku)`.

### Kho in-transit

- `WarehouseType.InTransit`
- Tự tạo: `IN_TRANSIT_{MÃ_BRAND}` hoặc `IN_TRANSIT_SHARED`
- Không dùng làm nguồn/đích khi tạo phiếu chuyển thủ công

---

## Vòng đời phiếu chuyển kho

```text
Draft
  → Approve (ship): OUT nguồn + IN in-transit → Approved / Shipped
  → receive-transfer: OUT in-transit + IN đích → Completed / Received
```

| API | Tác dụng |
|-----|----------|
| `POST .../transfer` | Tạo Draft, kiểm tra policy |
| `POST .../{id}/approve` | Xuất hàng (ship) |
| `POST .../{id}/receive-transfer` | Nhận tại kho đích |

---

## Omni-channel theo brand

Tham số tùy chọn trên API allocate / check availability:

- `brandId` — tra SKU theo brand, lọc kho
- `regionCode` — lọc kho theo vùng

Kho được chọn khi: `BrandId` null hoặc trùng brand; `RegionCode` null hoặc trùng vùng.

---

## Insights

Query `brandId`, `regionCode` trên:

- `GET /api/inventory-insights/dead-stock`
- `GET /api/inventory-insights/sales-velocity`
- `GET /api/inventory-insights/transfer-suggestions`

Gợi ý chuyển kho chỉ ghép kho **cùng brand** và **cùng region** (khi có gán region).

---

## Header phạm vi (RBAC-lite)

| Header | Định dạng |
|--------|-----------|
| `X-Brand-Id` | GUID |
| `X-Warehouse-Ids` | GUID cách nhau bởi dấu phẩy |
| `X-Region-Code` | Chuỗi |

Middleware `BrandScopeMiddleware` áp dụng cho fulfillment và insights khi request không truyền tham số tương ứng.

---

## Chính sách chuyển khác brand

Quản lý qua API admin:

```http
GET/POST /api/admin/transfer-policies
PUT    /api/admin/transfer-policies/{id}
```

Hoặc chèn SQL mẫu:

```sql
INSERT INTO transfer_policies (id, "SourceBrandId", "DestinationBrandId", "AllowCrossBrand", "IsActive", "Note")
VALUES (gen_random_uuid(), '<brand-nguon>', '<brand-dich>', true, true, 'Cho phep chuyen giua brand');
```

---

## File liên quan

Xem [MultiBrand.md](MultiBrand.md) (EN) — bảng đường dẫn source code đầy đủ.
