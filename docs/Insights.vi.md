# Inventory Insights (Phân tích tồn kho)

API và giao diện **chỉ đọc** hỗ trợ ra quyết định vận hành kho bán lẻ. Insights kết hợp tín hiệu sổ cái tồn, dữ liệu giá/định giá và ngữ cảnh mua hàng (PO/GR) — **không** ghi sổ tồn.

**Đường dẫn UI:** `/[locale]/insights` (mặc định `vi`)

**API:** `/api/inventory-insights/*`

---

## Tổng quan

| Tầng | Trách nhiệm |
|------|-------------|
| **Read repository** | Truy vấn read-model trên `CurrentStock`, `StockTransaction`, `ProductPrice`, `InventoryValuationSnapshot`, PO/GR |
| **App service** | Map sang DTO; có thể dùng cache `InsightSnapshot` |
| **Recommendation engine** | Thẻ hành động (CTA) theo quy tắc cho từng dòng insight |
| **Frontend** | Dải tổng quan điều hành + 7 tab phân tích, bộ lọc, liên kết drill-down |

Insights hiện tại là **quy tắc xác định**, chưa dùng AI. Phù hợp cho quản lý vận hành và lớp AI Copilot sau này.

---

## Bảng điều hành (Executive summary)

`GET /api/inventory-insights/executive-summary`

Tổng hợp KPI theo phạm vi lọc (`warehouseId`, `brandId`, `regionCode`):

- Số SKU tồn chết và giá trị tồn có rủi ro
- Số SKU bán nhanh
- Số gợi ý chuyển kho
- Số ứng viên giảm giá (markdown)
- Rủi ro khuyến mãi và rủi ro đặt hàng lại
- Xu hướng (delta tồn so với kỳ trước)

Frontend hiển thị qua **InsightsExecutiveSummaryStrip** phía trên thanh tab.

---

## 7 tab phân tích

| Tab | Endpoint | Mục đích |
|-----|----------|----------|
| **Tồn chết** | `GET .../dead-stock` | SKU còn tồn nhưng không có xuất trong N ngày |
| **Tốc độ bán** | `GET .../sales-velocity` | Tốc độ xuất và số ngày cover trong cửa sổ lookback |
| **Gợi ý chuyển** | `GET .../transfer-suggestions` | Chuyển từ kho thừa sang kho thiếu (cùng brand/vùng) |
| **Ứng viên markdown** | `GET .../markdown-candidates` | Hàng bán chậm kèm giá bán / biên lợi nhuận |
| **Rủi ro khuyến mãi** | `GET .../promotion-risk` | Giá KM đang/chuẩn bị hiệu lực vs tốc độ bán và cover |
| **Rủi ro đặt hàng** | `GET .../reorder-risk` | Cover thấp + tín hiệu PO/GR đang mở |
| **Xu hướng** | `GET .../trend-summary` | Delta tồn và luân chuyển giữa các kỳ |

Tham số chung:

- `warehouseId`, `brandId`, `regionCode` — lọc phạm vi (header `X-Brand-Id`, `X-Warehouse-Ids`, `X-Region-Code` khi không truyền param)
- `lookbackDays` — velocity, promotion, reorder, trend (mặc định 30)
- `daysWithoutOutbound` — tồn chết / markdown (mặc định 60)
- `minOnHand`, `maxResults` — giới hạn số dòng

---

## Trường nhận biết giá

DTO bổ sung giá vận hành và định giá cùng tín hiệu số lượng:

| Nhóm trường | Nguồn | Dùng trong |
|-------------|-------|------------|
| `CurrentSellingPriceBeforeVat`, `CurrentSellingPriceAfterVat`, `VatRate` | Cache SKU / `ProductPrice` | Tồn chết, velocity, chuyển kho, markdown |
| `CurrentCostPrice`, `GrossMarginPercent` | Cache giá vốn SKU | Markdown, rủi ro KM |
| `InventoryValueAtCost`, `InventoryValueAtSelling` | `InventoryValuationSnapshot` hoặc giá × tồn | Executive summary, tồn chết, markdown |
| Giá Promotion / Markdown | `ProductPrice` (`PriceType`) | Rủi ro KM, ứng viên markdown |

Insights **chỉ đọc** giá; không tạo `ProductPrice` hay đổi cache SKU.

---

## Hành động gợi ý (CTA)

`InsightRecommendationEngine` gắn **action** cho mỗi insight:

| Loại | Ví dụ đường dẫn |
|------|-----------------|
| Xem lịch sử tồn | `/[locale]/stock-history?...` |
| Xem SKU | `/[locale]/product-variants?...` |
| Mở báo cáo | `/[locale]/reports?...` |
| Soạn phiếu chuyển | `/[locale]/inventory-documents?type=transfer&...` |
| Soạn PO | `/[locale]/purchase-orders?...` |
| Áp dụng giảm giá | `/[locale]/product-variants?...` (prefill giá Markdown) |

Độ sâu giảm giá và giá đề xuất do **`MarkdownPolicyEngine`** (cấu hình tại `/admin/markdown-policies`). Xem [MarkdownPolicy.vi.md](MarkdownPolicy.vi.md).

Mã action: `InsightActionCodes`; loại: `InsightActionTypes`. UI dùng **RecommendationCard** và i18n `insights.recommendation.*` trong `frontend/messages/en.json` và `vi.json`.

---

## Cache snapshot

Truy vấn nặng có thể lấy từ `InsightSnapshot` (key qua `InsightSnapshotKeyBuilder`). Làm mới qua admin:

- `GET /api/admin/operations`
- `POST /api/admin/operations/jobs/{jobKey}` — job refresh insight

---

## Tài liệu liên quan

| File | Nội dung |
|------|----------|
| [Insights.md](Insights.md) | English version |
| [UseCases.md](UseCases.md) | UC012 — Inventory Insights |
| [BusinessRules.vi.md](BusinessRules.vi.md) | Quy tắc BR16xx |
| [MarkdownPolicy.vi.md](MarkdownPolicy.vi.md) | Chính sách giảm giá theo brand |
| [MultiBrand.vi.md](MultiBrand.vi.md) | Phạm vi brand/vùng cho insights |
| [InventoryDomain.md](InventoryDomain.md) | Vị trí trong domain |
