# Tài Liệu Tích Hợp Bán Hàng Đa Kênh (Omni-Channel Integration)

Tài liệu này mô tả kiến trúc và các API kết nối giữa **StockLedger Retail** và các hệ thống bán hàng ngoại vi: **POS tại cửa hàng**, **Hệ thống quản lý đơn hàng (OMS)**, **Sàn E-commerce (Shopee, Lazada, TikTok Shop)**, và **ERP**.

---

## 1. Tổng Quan Kiến Trúc Tích Hợp

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                             HỆ THỐNG NGOẠI VI                                │
│                                                                             │
│   ┌──────────────┐     ┌──────────────┐     ┌───────────────────────────┐   │
│   │  POS Stores  │     │     OMS      │     │  Ecom (Shopee, TikTok...) │   │
│   │ (Online/Off) │     │ (Fulfillment)│     │     (Sync Inventory)      │   │
│   └───────┬──────┘     └───────┬──────┘     └─────────────┬─────────────┘   │
└───────────┼────────────────────┼──────────────────────────┼─────────────────┘
            │                    │                          │
            ▼                    ▼                          ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          STOCKLEDGER RETAIL API                             │
│                                                                             │
│  1. Check ATP & Safety Stock Buffer (/api/integration/sales/check-...)      │
│  2. Real-time Sale/Return Confirm (/api/integration/sales/confirm-sale)     │
│  3. Batch Confirm Sales for Offline POS (/sales/batch-confirm-sales)        │
│  4. Delta Stock Sync Polling (/api/integration/stocks/delta)                │
│  5. Real-time Webhook Event Dispatcher (stock.changed)                      │
│                                                                             │
│  ┌──────────────────────┐  ┌──────────────────────┐  ┌───────────────────┐  │
│  │  CurrentStock Engine │  │  Stock Reservations  │  │ Inventory Ledger  │  │
│  └──────────────────────┘  └──────────────────────┘  └───────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Danh Sách Endpoint Tích Hợp

### 2.1 Kiểm tra tồn khả dụng & Đệm tồn an toàn (ATP with Safety Buffer)

* **Endpoint:** `POST /api/integration/sales/check-availability`
* **Công dụng:** Kiểm tra tồn khả dụng theo SKU trong kho cửa hàng/DC.
* **Hỗ trợ `SafetyStockBuffer`:** Trừ mức tồn đệm cho kênh online để tránh bán lấn vào hàng trưng bày tại cửa hàng.

**Request Body:**
```json
{
  "warehouseId": "a1b2c3d4-0000-0000-0000-000000000001",
  "safetyStockBuffer": 2.0,
  "lines": [
    { "sku": "NIKE-AIR-42", "quantity": 1 }
  ]
}
```

---

### 2.2 Xác nhận đơn bán hàng đơn lẻ (Realtime POS Confirm Sale)

* **Endpoint:** `POST /api/integration/sales/confirm-sale`
* **Tính chất:** **Idempotent** (gọi lại cùng `sourceSystem` + `orderReference` không trừ tồn 2 lần, trả kết quả phiếu đã tạo `isReplay: true`).
* **Hành động:** Tự động tạo phiếu xuất kho (`StockOut`), duyệt phiếu, trừ tồn `CurrentStock`, ghi nhận `StockTransaction` và kích hoạt bắn Webhook.

**Request Body:**
```json
{
  "sourceSystem": "POS",
  "orderReference": "POS-HCM01-20260831-0001",
  "warehouseId": "a1b2c3d4-0000-0000-0000-000000000001",
  "lines": [
    { "sku": "NIKE-AIR-42", "quantity": 1 }
  ]
}
```

---

### 2.3 Đồng bộ hàng loạt đơn bán hàng ngoại tuyến (Batch Confirm Sales for Offline POS)

* **Endpoint:** `POST /api/integration/sales/batch-confirm-sales`
* **Công dụng:** Khi POS mất mạng tại cửa hàng và lưu đơn cục bộ, khi có mạng lại sẽ gửi toàn bộ mảng đơn hàng lên hệ thống để trừ tồn hàng loạt.
* **Tính chất:** Xử lý an toàn từng đơn (Fault-tolerant), trả về chi tiết thành công/thất bại từng mã đơn.

**Request Body:**
```json
{
  "sourceSystem": "POS",
  "sales": [
    {
      "orderReference": "POS-OFFLINE-001",
      "warehouseId": "a1b2c3d4-0000-0000-0000-000000000001",
      "lines": [{ "sku": "SKU-A", "quantity": 2 }]
    },
    {
      "orderReference": "POS-OFFLINE-002",
      "warehouseId": "a1b2c3d4-0000-0000-0000-000000000001",
      "lines": [{ "sku": "SKU-B", "quantity": 1 }]
    }
  ]
}
```

**Response Body:**
```json
{
  "totalCount": 2,
  "successCount": 2,
  "failedCount": 0,
  "results": [
    {
      "orderReference": "POS-OFFLINE-001",
      "success": true,
      "data": { "documentNo": "OUT-20260831-0001", "isReplay": false }
    },
    {
      "orderReference": "POS-OFFLINE-002",
      "success": true,
      "data": { "documentNo": "OUT-20260831-0002", "isReplay": false }
    }
  ]
}
```

---

### 2.4 Đồng bộ biến động tồn kho (Delta Stock Sync Polling)

* **Endpoint:** `GET /api/integration/stocks/delta?sinceUtc=2026-08-31T08:00:00Z&limit=500`
* **Công dụng:** Dành cho OMS hoặc sàn E-com quét các SKU có biến động tồn kho kể từ một mốc thời gian `sinceUtc`.
* **Ưu điểm:** Không cần quét toàn bộ danh mục triệu SKU, chỉ lấy các SKU thực sự có giao dịch xuất/nhập/điều chỉnh/kiểm kê.

**Response Body:**
```json
{
  "sinceUtc": "2026-08-31T08:00:00Z",
  "generatedAtUtc": "2026-08-31T08:30:00Z",
  "totalChanges": 1,
  "hasMore": false,
  "items": [
    {
      "warehouseId": "a1b2c3d4-0000-0000-0000-000000000001",
      "warehouseCode": "STORE-HCM-01",
      "warehouseName": "Cửa hàng Nguyễn Trãi",
      "productVariantId": "b2c3d4e5-0000-0000-0000-000000000002",
      "sku": "NIKE-AIR-42",
      "productName": "Nike Air Zoom Pegasus 42",
      "size": "42",
      "color": "Black/White",
      "onHandQuantity": 15,
      "reservedQuantity": 1,
      "availableQuantity": 14,
      "lastMovementAtUtc": "2026-08-31T08:15:22Z"
    }
  ]
}
```

---

### 2.5 Webhook Sự Kiện Thay Đổi Tồn Kho (Stock Changed Webhook)

* **Sự kiện:** `stock.changed`
* **Cấu hình (`appsettings.json`):**
```json
"Integration": {
  "Webhooks": {
    "Enabled": true,
    "Endpoints": [
      "https://ecom-sync.acfc.com.vn/api/webhooks/stock-changed",
      "https://oms.acfc.com.vn/api/webhooks/inventory"
    ],
    "TimeoutSeconds": 5,
    "SecretKey": "ACFC_WEBHOOK_SECRET_KEY_FOR_HMAC_SHA256"
  }
}
```
* **Bảo mật:** Gửi kèm header `X-StockLedger-Signature` được ký bằng HMAC-SHA256 để hệ thống nhận xác minh tính toàn vẹn.
* **Test Endpoint:** `POST /api/integration/stocks/webhooks/test`
