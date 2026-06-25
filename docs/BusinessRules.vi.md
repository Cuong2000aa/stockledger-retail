# Business Rules (Quy tắc nghiệp vụ)

---

# General Rules (Quy tắc chung)

| Rule Code | English                                                  | Tiếng Việt                                        |
| --------- | -------------------------------------------------------- | ------------------------------------------------- |
| BR001     | Every inventory movement must create a StockTransaction. | Mọi biến động tồn kho phải sinh StockTransaction. |
| BR002     | Only approved documents can affect inventory.            | Chỉ phiếu đã duyệt mới được tác động tồn kho.     |
| BR003     | Draft documents must not affect inventory.               | Phiếu nháp không được ảnh hưởng tồn kho.          |
| BR004     | Cancelled documents must not affect inventory.           | Phiếu hủy không được ảnh hưởng tồn kho.           |
| BR005     | Inventory quantity cannot become negative.               | Không cho phép tồn kho âm.                        |
| BR006     | ProductVariant must exist.                               | SKU phải tồn tại.                                 |
| BR007     | Warehouse must exist.                                    | Kho phải tồn tại.                                 |

---

# Stock In (Nhập kho)

## Mục đích

Nhập hàng từ nhà cung cấp hoặc nguồn khác vào kho.

### Ví dụ

```text
Nhà cung cấp giao 100 áo Polo.
```

---

## Rules

| Rule Code | English                             | Tiếng Việt                   |
| --------- | ----------------------------------- | ---------------------------- |
| BR101     | Destination warehouse is required.  | Bắt buộc chọn kho nhận hàng. |
| BR102     | Quantity must be greater than zero. | Số lượng phải lớn hơn 0.     |
| BR103     | Create IN transaction.              | Sinh giao dịch IN.           |
| BR104     | Increase CurrentStock.              | Tăng tồn kho hiện tại.       |

---

# Stock Out (Xuất kho)

## Mục đích

Xuất hàng khỏi kho.

### Ví dụ

```text
Xuất bán hàng.

Xuất hàng demo.

Xuất hủy.
```

---

## Rules

| Rule Code | English                             | Tiếng Việt               |
| --------- | ----------------------------------- | ------------------------ |
| BR201     | Source warehouse is required.       | Bắt buộc chọn kho xuất.  |
| BR202     | Quantity must be greater than zero. | Số lượng phải lớn hơn 0. |
| BR203     | Available stock must be sufficient. | Tồn khả dụng phải đủ.    |
| BR204     | Create OUT transaction.             | Sinh giao dịch OUT.      |
| BR205     | Decrease CurrentStock.              | Giảm tồn kho hiện tại.   |

---

# Transfer (Chuyển kho)

## Mục đích

Chuyển hàng giữa hai kho.

### Ví dụ

```text
Store Q1
↓
Store Q7
```

---

## Rules

| Rule Code | English                                          | Tiếng Việt                                         |
| --------- | ------------------------------------------------ | -------------------------------------------------- |
| BR301     | Source warehouse is required.                    | Bắt buộc có kho nguồn.                             |
| BR302     | Destination warehouse is required.               | Bắt buộc có kho đích.                              |
| BR303     | Source and destination cannot be the same.       | Kho nguồn và kho đích không được trùng nhau.       |
| BR304     | Source warehouse must have enough stock.         | Kho nguồn phải đủ tồn kho.                         |
| BR305     | Create TRANSFER_OUT transaction.                 | Sinh giao dịch TRANSFER_OUT.                       |
| BR306     | Create TRANSFER_IN transaction.                  | Sinh giao dịch TRANSFER_IN.                        |
| BR307     | One transfer step creates two stock transactions. | Mỗi bước ship/receive tạo hai giao dịch. |
| BR308     | Cross-brand transfer needs active TransferPolicy. | Chuyển khác brand cần policy AllowCrossBrand. |
| BR309     | In-transit warehouse not allowed as transfer endpoint. | Không dùng kho in-transit làm nguồn/đích thủ công. |
| BR310     | Approve = ship; receive-transfer = nhận tại đích. | Approve xuất hàng; receive nhận tại kho đích. |
| BR311     | SKU brand must match warehouse brand scope. | Brand SKU phải tương thích brand kho. |

---

# Adjustment (Điều chỉnh tồn kho)

## Mục đích

Điều chỉnh tăng hoặc giảm tồn kho.

### Ví dụ

```text
Mất hàng.

Tìm thấy hàng.

Sai lệch dữ liệu.
```

---

## Rules

| Rule Code | English                                      | Tiếng Việt                            |
| --------- | -------------------------------------------- | ------------------------------------- |
| BR401     | Adjustment reason is required.               | Bắt buộc nhập lý do điều chỉnh.       |
| BR402     | Positive adjustment creates ADJUSTMENT_IN.   | Điều chỉnh tăng sinh ADJUSTMENT_IN.   |
| BR403     | Negative adjustment creates ADJUSTMENT_OUT.  | Điều chỉnh giảm sinh ADJUSTMENT_OUT.  |
| BR404     | Adjustment cannot create negative inventory. | Điều chỉnh không được làm tồn kho âm. |

---

# Stock Count (Kiểm kê)

## Mục đích

Đối chiếu tồn hệ thống và tồn thực tế.

### Ví dụ

```text
Tồn hệ thống = 100

Tồn thực tế = 98
```

Chênh lệch:

```text
-2
```

---

## Rules

| Rule Code | English                                         | Tiếng Việt                                   |
| --------- | ----------------------------------------------- | -------------------------------------------- |
| BR501     | Counted quantity cannot be negative.            | Số lượng kiểm kê không được âm.              |
| BR502     | Variance = Counted - System Quantity.           | Chênh lệch = Tồn thực tế - Tồn hệ thống.     |
| BR503     | Positive variance creates COUNT_ADJUSTMENT_IN.  | Chênh lệch dương sinh COUNT_ADJUSTMENT_IN.   |
| BR504     | Negative variance creates COUNT_ADJUSTMENT_OUT. | Chênh lệch âm sinh COUNT_ADJUSTMENT_OUT.     |
| BR505     | No transaction if variance is zero.             | Không tạo giao dịch nếu không có chênh lệch. |

---

# Current Stock Rules (Quy tắc tồn kho hiện tại)

| Rule Code | English                                              | Tiếng Việt                                            |
| --------- | ---------------------------------------------------- | ----------------------------------------------------- |
| BR601     | CurrentStock must be updated after StockTransaction. | CurrentStock phải được cập nhật sau StockTransaction. |
| BR602     | Available = OnHand - Reserved.                       | Tồn khả dụng = Tồn thực tế - Tồn giữ chỗ.             |
| BR603     | One CurrentStock per ProductVariant and Warehouse.   | Mỗi SKU tại mỗi kho chỉ có một CurrentStock.          |
| BR604     | LastTransactionId should be stored.                  | Nên lưu LastTransactionId để truy vết.                |

---

# Audit Rules (Quy tắc ghi nhận lịch sử)

| Rule Code | English                                       | Tiếng Việt                                  |
| --------- | --------------------------------------------- | ------------------------------------------- |
| BR701     | Important actions must be logged.             | Các thao tác quan trọng phải được ghi log.  |
| BR702     | Approval actions must be logged.              | Hành động duyệt phiếu phải được ghi log.    |
| BR703     | Cancellation actions must be logged.          | Hành động hủy phiếu phải được ghi log.      |
| BR704     | Data changes should store old and new values. | Thay đổi dữ liệu nên lưu giá trị cũ và mới. |

---

# Golden Rule (Nguyên tắc vàng)

```text
InventoryDocument
        ↓
InventoryDocumentLine
        ↓
StockTransaction
        ↓
CurrentStock
```

Không bao giờ update CurrentStock trực tiếp mà không tạo StockTransaction.

---

# Draft Update (Cập nhật phiếu nháp)

| Rule Code | English | Tiếng Việt |
| --------- | ------- | ---------- |
| BR801 | Only Draft inventory documents can be updated. | Chỉ phiếu Draft mới được sửa. |
| BR802 | Approved or Cancelled documents are immutable. | Phiếu đã duyệt hoặc hủy không được sửa. |
| BR803 | Update replaces line items as provided. | Cập nhật thay thế toàn bộ dòng hàng theo payload. |
| BR804 | Updating draft must not create StockTransaction. | Sửa nháp không sinh giao dịch tồn kho. |

---

# Purchase Order (Đơn mua hàng)

| Rule Code | English | Tiếng Việt |
| --------- | ------- | ---------- |
| BR901 | Supplier and warehouse are required. | Bắt buộc nhà cung cấp và kho nhận. |
| BR902 | At least one line with OrderedQuantity > 0. | Ít nhất một dòng với số lượng đặt > 0. |
| BR903 | Only Draft PO can be edited. | Chỉ PO nháp mới sửa được. |
| BR904 | Submit: Draft → Submitted. | Gửi đơn: Draft → Submitted. |
| BR905 | Cancel only when nothing received yet. | Hủy chỉ khi chưa nhận hàng. |
| BR906 | PO does not affect stock. | PO không tác động tồn kho. |
| BR907 | GR approval updates PO ReceivedQuantity. | Duyệt GR cập nhật số đã nhận trên PO. |
| BR908 | PO → PartiallyReceived or Received. | PO chuyển trạng thái theo số đã nhận. |

---

# Goods Receipt (Phiếu nhận hàng)

| Rule Code | English | Tiếng Việt |
| --------- | ------- | ---------- |
| BR1001 | GR must link to submitted PO. | GR phải gắn PO đã gửi. |
| BR1002 | Received qty ≤ remaining on PO line. | Số nhận ≤ số còn lại trên dòng PO. |
| BR1003 | Approve creates Stock In (PROCUREMENT). | Duyệt tạo phiếu nhập (SourceSystem = PROCUREMENT). |
| BR1004 | Stock In auto-approved with GR. | Phiếu nhập được duyệt cùng lúc với GR. |
| BR1005 | GR stores InventoryDocumentId. | GR lưu liên kết phiếu nhập. |
| BR1006 | Only Draft GR can approve/cancel. | Chỉ GR nháp mới duyệt/hủy được. |

---

# POS Integration (Tích hợp bán hàng)

| Rule Code | English | Tiếng Việt |
| --------- | ------- | ---------- |
| BR1101 | check-availability is read-only. | Kiểm tra tồn chỉ đọc, không ghi sổ. |
| BR1102 | confirm-sale → Stock Out + approve. | Xác nhận bán → xuất kho + duyệt. |
| BR1103 | confirm-return → Stock In + approve. | Xác nhận trả → nhập kho + duyệt. |
| BR1104 | Idempotent by SourceSystem + ReferenceNo. | Gọi lại cùng mã tham chiếu không trừ/cộng 2 lần. |
| BR1105 | Duplicate returns existing document. | Trùng tham chiếu trả về phiếu đã tạo. |
| BR1106 | Sale validates available stock. | Bán phải đủ tồn khả dụng. |

---

# Inventory Valuation (Định giá tồn kho)

| Rule Code | English | Tiếng Việt |
| --------- | ------- | ---------- |
| BR1201 | Legacy CostPrice, SellingPrice optional on SKU. | CostPrice, SellingPrice cũ trên SKU vẫn có thể để trống để tương thích ngược. |
| BR1202 | CurrentCostPrice, CurrentSellingPrice, VAT selling fields, and CurrentCostSource are SKU current cache fields. | CurrentCostPrice, CurrentSellingPrice, giá trước/sau VAT và CurrentCostSource là cache hiện hành trên SKU. |
| BR1203 | CostSource: Manual, Erp, Pos, PurchaseSystem. | Nguồn giá vốn theo enum CostSource. |
| BR1204 | ProductCostHistory has EffectiveFrom/To and IsCurrent. | ProductCostHistory có EffectiveFrom/To và cờ IsCurrent. |
| BR1205 | ProductCostHistory readable via reports API. | Đọc lịch sử giá vốn qua `GET /api/reports/cost-history`. |
| BR1206 | Negative prices not allowed. | Không cho giá âm. |
| BR1207 | VAT rate must be between 0 and 100. | Thuế VAT phải nằm trong khoảng 0 đến 100. |
| BR1208 | ProductPrice stores effective-dated selling prices by PriceType. | ProductPrice lưu giá bán theo thời gian hiệu lực và theo PriceType. |
| BR1209 | Updating current Regular price refreshes SKU current selling cache. | Cập nhật Regular price đang hiệu lực sẽ làm mới cache giá bán hiện hành trên SKU. |
| BR1210 | Promotion and Markdown keep separate effective histories. | Promotion và Markdown giữ lịch sử hiệu lực riêng, không ghi đè current của nhau. |
| BR1211 | InventoryValuationSnapshot stores valuation per SKU / warehouse / date. | InventoryValuationSnapshot lưu định giá theo SKU / kho / ngày snapshot. |

---

# Multi-Brand (Đa thương hiệu)

| Rule Code | English | Tiếng Việt |
| --------- | ------- | ---------- |
| BR1301 | Brand Code unique. | Mã brand duy nhất. |
| BR1302 | SKU unique per (BrandId, Sku). | SKU unique theo brand. |
| BR1303 | Warehouse brand must match SKU brand scope. | Brand kho phải khớp phạm vi SKU. |
| BR1304 | Fulfillment/insights filter by brandId, regionCode. | Allocate/insights lọc brand/vùng. |
| BR1305 | Scope headers apply when params omitted. | Header phạm vi khi không truyền param. |
| BR1306 | Transfer suggestions same brand + region. | Gợi ý chuyển cùng brand và vùng. |

---

# Quy tắc Insights (Phân tích tồn kho)

| Mã | Rule | Mô tả |
|----|------|-------|
| BR1601 | Insights chỉ đọc; không tạo phiếu hay ghi `StockTransaction`. | API insights không ghi sổ. |
| BR1602 | Lọc theo `warehouseId`, `brandId`, `regionCode`; header phạm vi khi thiếu param. | Phạm vi brand/kho/vùng. |
| BR1603 | Tồn chết / markdown: tồn ≥ `minOnHand`, không xuất trong `daysWithoutOutbound`. | Điều kiện tồn chết. |
| BR1604 | Gợi ý chuyển: cặp kho cùng brand/vùng; số lượng từ thừa vs cover mục tiêu. | Logic chuyển kho. |
| BR1605 | Giá trên DTO lấy từ cache SKU, `ProductPrice`, `InventoryValuationSnapshot`; không sửa giá. | Insights chỉ đọc giá. |
| BR1606 | Rủi ro KM: giá Promotion/Markdown đang hoặc vừa hiệu lực. | Tín hiệu khuyến mãi. |
| BR1607 | Rủi ro đặt hàng: cover thấp + PO/GR đang mở. | Tín hiệu mua hàng. |
| BR1608 | Xu hướng: so sánh kỳ lookback với kỳ trước cùng độ dài. | Delta theo kỳ. |
| BR1609 | Mỗi dòng có thể có 0+ CTA; chỉ deep-link tới màn hình hiện có. | Hành động gợi ý. |
| BR1610 | Có thể cache `InsightSnapshot`; admin refresh qua operations. | Cache snapshot. |

Chi tiết: [Insights.vi.md](Insights.vi.md)

---

# Quy tắc duyệt nhiều bước (Approval Workflow)

| Mã | Rule | Mô tả |
|----|------|-------|
| BR1401 | High-value docs need submit-for-approval. | Phiếu vượt ngưỡng phải gửi duyệt trước. |
| BR1402 | Multiple approval steps allowed. | Có thể yêu cầu nhiều bước duyệt. |
| BR1403 | Stock posts after all steps. | Chỉ ghi sổ khi đủ bước duyệt. |
| BR1404 | PO PendingApproval on submit. | PO lớn chuyển `PendingApproval` khi submit. |

---

# Quy tắc lô / HSD

| Mã | Rule | Mô tả |
|----|------|-------|
| BR1501 | TrackLotExpiry enables lots. | SKU bật theo dõi lô/HSD. |
| BR1502 | FEFO on outbound. | Xuất kho ưu tiên lô sắp hết hạn. |
| BR1503 | Near-expiry report. | Báo cáo lô cận hạn theo số ngày. |
