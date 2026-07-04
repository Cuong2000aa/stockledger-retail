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
| **Frontend** | Dải tổng quan điều hành + **9 tab** phân tích, bộ lọc, explain modal, bulk transfer |

Insights hiện tại là **quy tắc xác định**, chưa dùng AI. `POST .../explain` giải thích theo rule (không phải LLM).

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
- Số broken size run và season clearance (khi tab đã tải)

Frontend hiển thị qua **InsightsExecutiveSummaryStrip** phía trên thanh tab.

---

## 9 tab phân tích

### Vận hành (7)

| Tab | Endpoint | Mục đích |
|-----|----------|----------|
| **Tồn chết** | `GET .../dead-stock` | SKU còn tồn nhưng không có xuất trong N ngày |
| **Tốc độ bán** | `GET .../sales-velocity` | Tốc độ xuất và số ngày cover trong cửa sổ lookback |
| **Gợi ý chuyển** | `GET .../transfer-suggestions` | Chuyển từ kho thừa sang kho thiếu (cùng brand/vùng) |
| **Ứng viên markdown** | `GET .../markdown-candidates` | Hàng bán chậm kèm giá bán / biên lợi nhuận |
| **Rủi ro khuyến mãi** | `GET .../promotion-risk` | Giá KM đang/chuẩn bị hiệu lực vs tốc độ bán và cover |
| **Rủi ro đặt hàng** | `GET .../reorder-risk` | Cover thấp + tín hiệu PO/GR đang mở |
| **Xu hướng** | `GET .../trend-summary` | Delta tồn và luân chuyển giữa các kỳ |

### Thời trang (2)

| Tab | Endpoint | Mục đích |
|-----|----------|----------|
| **Thiếu size** | `GET .../broken-size-runs` | Sản phẩm thiếu size trong dải (VD: có S/L, thiếu M) |
| **Xả mùa** | `GET .../season-clearance` | SKU mùa cũ bán chậm, gợi ý giá clearance |

Tham số chung:

- `warehouseId`, `brandId`, `regionCode` — lọc phạm vi (header `X-Brand-Id`, `X-Warehouse-Ids`, `X-Region-Code` khi không truyền param)
- `lookbackDays` — velocity, promotion, reorder, trend, fashion (mặc định 30)
- `daysWithoutOutbound` — tồn chết / markdown / xả mùa (mặc định 60)
- `currentSeason` — lọc mùa cho tab xả mùa (tùy chọn)
- `minOnHand`, `maxResults` — giới hạn số dòng

**Tồn chết phân trang:** `GET .../dead-stock/paged` — thêm `page`, `pageSize`.

---

## API tương tác

| Method | Path | Mục đích |
|--------|------|----------|
| `POST` | `.../explain` | Giải thích rule cho một dòng insight (modal trên UI) |
| `POST` | `.../markdown-what-if` | Mô phỏng % giảm / thu hồi vốn, không lưu giá |
| `POST` | `.../bulk-transfers` | Tạo phiếu chuyển Draft từ gợi ý đã chọn |

`bulk-transfers` chỉ tạo phiếu **Draft**; chưa ghi sổ cho đến khi duyệt.

---

## Theo dõi hành động insight

| Method | Path | Mục đích |
|--------|------|---------|
| `POST` | `/api/insight-actions` | Ghi nhận click CTA |
| `GET` | `/api/insight-actions/recent` | Hành động gần đây |
| `GET` | `/api/insight-actions/stats` | Thống kê theo `lookbackDays` |

Lưu tại bảng `insight_action_logs`.

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

## Cache snapshot & job nền

Truy vấn nặng có thể lấy từ `InsightSnapshot` (key qua `InsightSnapshotKeyBuilder`).

| Job key | Mục đích |
|---------|----------|
| `insight_snapshots` | Làm mới cache insight |
| `insight_alerts` | Cảnh báo ngưỡng (tồn chết, vốn tồn, reorder) |
| `inventory_daily_rollups` | Rollup KPI hàng ngày |

Làm mới / theo dõi qua admin:

- `GET /api/admin/operations`
- `POST /api/admin/operations/jobs/{jobKey}/run`

---

## Tài liệu liên quan

| File | Nội dung |
|------|----------|
| [Insights.md](Insights.md) | English version |
| [UseCases.md](UseCases.md) | UC012, UC022–UC024 |
| [BusinessRules.vi.md](BusinessRules.vi.md) | Quy tắc BR16xx |
| [MarkdownPolicy.vi.md](MarkdownPolicy.vi.md) | Chính sách giảm giá theo brand |
| [MultiBrand.vi.md](MultiBrand.vi.md) | Phạm vi brand/vùng cho insights |
| [InventoryDomain.md](InventoryDomain.md) | Vị trí trong domain |
