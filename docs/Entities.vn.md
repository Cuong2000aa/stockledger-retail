# Entities Dictionary

| Entity                | Tiếng Việt              | Mô tả                                 |
| --------------------- | ----------------------- | ------------------------------------- |
| Product               | Sản phẩm cha            | Thông tin sản phẩm tổng quát          |
| ProductVariant        | SKU / Biến thể sản phẩm | Đơn vị quản lý tồn kho thực tế        |
| Warehouse             | Kho                     | Kho tổng, cửa hàng hoặc kho con       |
| CurrentStock          | Tồn kho hiện tại        | Tồn thực tế đang có                   |
| InventoryDocument     | Phiếu nghiệp vụ         | Phiếu nhập, xuất, chuyển kho, kiểm kê |
| InventoryDocumentLine | Chi tiết phiếu          | Danh sách SKU trong phiếu             |
| StockTransaction      | Sổ cái tồn kho          | Lịch sử biến động tồn kho             |
| TransactionLog        | Nhật ký hệ thống        | Lịch sử thao tác người dùng           |
| Supplier              | Nhà cung cấp            | Đối tác mua hàng                      |
| PurchaseOrder         | Đơn mua hàng (PO)       | Đặt hàng từ NCC, chưa tác động tồn    |
| PurchaseOrderLine     | Chi tiết PO             | SKU và số lượng đặt / đã nhận        |
| GoodsReceipt          | Phiếu nhận hàng (GR)    | Nhận hàng theo PO → sinh phiếu nhập   |
| GoodsReceiptLine      | Chi tiết GR             | Số lượng nhận theo dòng PO            |
| ProductCostHistory    | Lịch sử giá vốn         | Đọc qua `/api/reports/cost-history` |
| ProductPrice          | Lịch sử giá bán         | Giá bán theo loại giá và hiệu lực |
| InventoryValuationSnapshot | Snapshot định giá tồn | Định giá tồn theo SKU / kho / ngày |
| StockLot              | Lô hàng                 | Mã lô, HSD theo SKU |
| LotStock              | Tồn theo lô             | Số lượng lô tại từng kho |
| Brand                 | Thương hiệu             | Master đa brand                      |
| TransferPolicy        | Chính sách chuyển kho   | Quy tắc chuyển khác brand             |
| MarkdownPolicy        | Chính sách giảm giá     | Tier markdown theo brand              |

---

# Brand (Thương hiệu)

* Code (duy nhất), Name, Status
* API: `/api/brands`
* Product / SKU / Warehouse có thể gắn `BrandId`

---

# TransferPolicy (Chính sách chuyển kho)

* Cho phép chuyển giữa kho khác brand khi `AllowCrossBrand = true`
* Cùng brand: không cần policy

---

# MarkdownPolicy (Chính sách giảm giá)

* Cấu hình theo `BrandId` (tùy chọn `RegionCode`, `WarehouseType`)
* `TiersJson`: các bậc % giảm theo ngày tồn / sell-through
* Engine: `MarkdownPolicyEngine` — chi tiết [MarkdownPolicy.vi.md](MarkdownPolicy.vi.md)

---

# Product (Sản phẩm cha)

## Mục đích

Lưu thông tin sản phẩm tổng quát.

## Ví dụ

Áo Polo Nam

Giày Sneaker XYZ

Túi Xách ABC

## Quan hệ

```text
Product
    |
    | 1 - N
    |
ProductVariant
```

---

# ProductVariant (SKU)

## Mục đích

Đơn vị quản lý tồn kho thực tế.

## Ví dụ

Product:

Áo Polo Nam

Variants:

* POLO-BLK-M
* POLO-BLK-L
* POLO-WHT-M
* POLO-WHT-L

## Ghi chú

Tồn kho luôn nằm ở ProductVariant.

Không nằm ở Product.

## Trường current pricing / cost cache

* CostPrice — giá vốn cũ để tương thích ngược
* SellingPrice — giá bán cũ để tương thích ngược
* CostSource — Manual, Erp, Pos, PurchaseSystem
* CurrentCostPrice — giá vốn hiện hành
* CurrentSellingPrice — giá bán hiện hành
* CurrentSellingPriceBeforeVat — giá trước VAT
* CurrentSellingPriceAfterVat — giá sau VAT
* VatRate — VAT mặc định của SKU
* CurrentCostSource — nguồn giá vốn hiện hành

Các trường này là current cache để đọc nhanh. Lịch sử giá và định giá chi tiết được tách sang entity riêng.

---

# Warehouse (Kho)

## Mục đích

Lưu thông tin kho.

## Loại kho

* DC (Kho tổng)
* Store (Cửa hàng)
* Sub Warehouse (Kho con)
* Defect (Kho hàng lỗi)
* Return (Kho hàng trả)
* InTransit (Kho đang luân chuyển — tự tạo theo brand)

## Trường bổ sung

* BrandId — brand sở hữu kho (null = dùng chung)
* RegionCode — mã vùng (HCM, HN...)
* FulfillmentPriority — ưu tiên allocate (số nhỏ = ưu tiên cao)

## Ví dụ

```text
Store Q1
├── Selling Area
├── Backroom
├── Return Area
└── Defect Area
```

---

# CurrentStock (Tồn hiện tại)

## Mục đích

Lưu tồn kho hiện tại.

## Ví dụ

```text
SKU: POLO-BLK-M

Store Q1

On Hand: 50
Reserved: 5
Available: 45
```

## Công thức

```text
Available = OnHand - Reserved
```

---

# InventoryDocument (Phiếu nghiệp vụ)

## Mục đích

Lưu thông tin phiếu nghiệp vụ.

## Các loại phiếu

### STOCK_IN

Phiếu nhập kho.

### STOCK_OUT

Phiếu xuất kho.

### TRANSFER

Phiếu chuyển kho.

### ADJUSTMENT

Phiếu điều chỉnh.

### STOCK_COUNT

Phiếu kiểm kê.

## Trạng thái

Draft → Approved (hoặc Cancelled từ Draft).

## SourceSystem

Mã nguồn tích hợp (ví dụ POS, PROCUREMENT) — dùng cho idempotent khi tích hợp hệ thống ngoài.

---

# InventoryDocumentLine (Chi tiết phiếu)

## Mục đích

Lưu danh sách SKU trong phiếu.

## Ví dụ

Phiếu:

PN00001

Chi tiết:

* POLO-BLK-M : 100
* POLO-WHT-M : 50

---

# StockTransaction (Sổ cái tồn kho)

## Mục đích

Lưu toàn bộ lịch sử biến động tồn kho.

## Ví dụ

### Nhập kho

```text
+100
```

### Bán hàng

```text
-5
```

### Điều chỉnh

```text
+2
```

### Chuyển kho

```text
Store Q1 -> Store Q7

TRANSFER_OUT = -10

TRANSFER_IN = +10
```

## Vai trò

Cho phép audit:

* Ai thao tác?
* Khi nào?
* Từ phiếu nào?
* Tồn thay đổi ra sao?

---

# TransactionLog (Nhật ký hệ thống)

## Mục đích

Lưu lịch sử thao tác hệ thống.

## Ví dụ

* Tạo sản phẩm
* Sửa sản phẩm
* Duyệt phiếu
* Hủy phiếu

## Khác với StockTransaction

StockTransaction:

* Lịch sử biến động tồn kho

TransactionLog:

* Lịch sử thao tác người dùng

---

# Supplier (Nhà cung cấp)

## Mục đích

Quản lý đối tác cung cấp hàng cho chuỗi mua hàng.

## Trường chính

Code, Name, ContactName, Phone, Email, Address, Status

---

# PurchaseOrder (Đơn mua hàng)

## Mục đích

Đặt hàng từ nhà cung cấp về kho. **PO không trừ/cộng tồn** — chỉ khi nhận hàng (GR) mới nhập kho.

## Luồng trạng thái

```text
Draft → Submitted → PartiallyReceived → Received
                 ↘ Cancelled (nếu chưa nhận)
```

## PurchaseOrderLine

* OrderedQuantity — số đặt
* ReceivedQuantity — số đã nhận (cập nhật khi duyệt GR)
* UnitCost — đơn giá (tùy chọn)

---

# GoodsReceipt (Phiếu nhận hàng)

## Mục đích

Ghi nhận hàng thực nhận theo PO.

## Khi duyệt

1. Tạo và duyệt phiếu nhập kho (SourceSystem = PROCUREMENT)
2. Cập nhật ReceivedQuantity trên PO
3. Lưu InventoryDocumentId trên GR

## GoodsReceiptLine

Liên kết PurchaseOrderLineId, ProductVariantId, ReceivedQuantity

---

# ProductCostHistory (Lịch sử giá vốn)

## Mục đích

Lưu giá vốn theo khoảng thời gian (EffectiveFrom / EffectiveTo).

## Ghi chú

Bảng lưu lịch sử giá vốn, có thêm `ValuationMethod`, `Currency`, `ReferenceType`, `ReferenceId`, `IsCurrent`; đọc qua `GET /api/reports/cost-history`.

---

# ProductPrice (Lịch sử giá bán)

## Mục đích

Lưu giá bán theo từng loại giá và thời gian hiệu lực.

## Trường chính

* PriceType — `Regular`, `Promotion`, `Markdown`, `Clearance`, `Channel`
* PriceBeforeVat
* VatRate
* PriceAfterVat
* Currency
* EffectiveFrom
* EffectiveTo
* IsCurrent

## Ghi chú

Regular Price đang hiệu lực sẽ cập nhật current selling price cache trên SKU.

Promotion và Markdown có lịch sử riêng, không ghi đè lẫn nhau.

---

# InventoryValuationSnapshot (Snapshot định giá tồn)

## Mục đích

Lưu snapshot định giá tồn kho theo SKU / kho / ngày để phục vụ báo cáo và analytics.

## Trường chính

* QuantityOnHand
* QuantityReserved
* QuantityAvailable
* AverageCost
* InventoryValue
* SnapshotDate
* ValuationMethod
* Currency

---

# Nguyên tắc thiết kế

## Rule 1

Mọi thay đổi tồn kho phải sinh StockTransaction.

## Rule 2

CurrentStock dùng để truy vấn nhanh.

## Rule 3

StockTransaction dùng để audit.

## Rule 4

Không cho tồn âm.

## Rule 5

Mọi giao dịch phải truy vết được.

## Rule 6

Mọi phiếu phải có ít nhất một dòng hàng.
