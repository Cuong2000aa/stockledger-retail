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
