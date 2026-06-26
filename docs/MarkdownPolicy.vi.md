# Chính sách giảm giá (Markdown Policy)

Cấu hình **rule giảm giá / xả hàng** theo **brand**. Engine đọc policy để đề xuất trên **Insights** (tồn chết, ứng viên markdown) và cung cấp CTA **Áp dụng giảm giá** dẫn tới màn chỉnh giá SKU.

**UI quản trị:** `/[locale]/admin/markdown-policies`  
**API:** `/api/admin/markdown-policies`

---

## Mục đích

Câu hỏi vận hành cần trả lời thống nhất:

- Hàng tồn bao lâu thì nên giảm giá?
- **Cửa hàng** tháng này bán brand đó nhanh hay chậm so với median brand?
- Giảm bao nhiêu % vẫn giữ **biên lợi nhuận** theo ngưỡng brand?
- Mỗi brand (Domino's, Popeyes, fashion…) có ngưỡng khác nhau ở đâu?

`MarkdownPolicy` gom các rule này. Insights vẫn **chỉ đọc** tồn; áp giá thực tế qua `ProductPrice` (`PriceType.Markdown`) trên màn SKU.

---

## Mô hình dữ liệu

| Trường | Mô tả |
|--------|--------|
| `BrandId` | Bắt buộc — policy mặc định theo brand (có thể override `RegionCode` / `WarehouseType`) |
| `LookbackDays` | Cửa sổ tính xuất bán / sell-through (mặc định 30 ngày) |
| `MinDaysWithoutOutbound` | Tối thiểu ngày không bán mới xét markdown |
| `MinOnHand` | Tồn tối thiểu |
| `MinInventoryValueAtCost` | Tùy chọn — bỏ qua dòng giá trị thấp |
| `MinGrossMarginPercent` | Sàn biên lợi nhuận sau giảm |
| `MaxMarkdownPercent` | Trần % giảm đề xuất |
| `AllowBelowCost` | Cho phép clearance dưới giá vốn |
| `RequireApprovalAbovePercent` | Gợi ý cần duyệt (cờ trên DTO) |
| `SlowSellThroughThreshold` | Tỷ lệ so với median sell-through brand; dưới ngưỡng → % tier sâu hơn |
| `TiersJson` | Mảng JSON các bậc giảm |

### Bậc (`MarkdownPolicyTier`)

| Trường | Mô tả |
|--------|--------|
| `TierCode` | Ví dụ `watch`, `moderate`, `aggressive` |
| `MinDaysWithoutOutbound` | Bậc bắt đầu từ số ngày đứng |
| `MaxDaysWithoutOutbound` | Ngày tối đa (tùy chọn) |
| `MarkdownPercent` | % giảm cơ bản |
| `SlowSellThroughMarkdownPercent` | Khi bán chậm so với brand |
| `Severity` | `warning` / `critical` cho UI Insights |

---

## Engine đánh giá

`MarkdownPolicyEngine.Evaluate` dùng:

1. **Thời gian** — ngày không xuất bán, ngưỡng policy
2. **Tốc độ shop** — xuất trong lookback ÷ tồn → **sell-through**
3. **Brand** — median sell-through các SKU cùng brand
4. **Giá** — giá bán, giá vốn, VAT
5. **Policy** — resolve theo brand, ưu tiên region / loại kho cụ thể hơn

Luồng:

1. Chọn policy active (hoặc default 10% / 15% / 25%).
2. Chọn tier khớp số ngày đứng.
3. Nếu sell-through shop &lt; ngưỡng × median brand → dùng % bán chậm.
4. Clamp `MaxMarkdownPercent`, nâng giá theo `MinGrossMarginPercent`.
5. Trả `MarkdownSuggestionDto` (%, giá đề xuất, tier, cờ duyệt).

---

## Tích hợp Insights

| Tab | Dùng policy |
|-----|-------------|
| **Tồn chết** | Giá markdown đề xuất + CTA khi `markdown_or_transfer` / `critical_markdown` |
| **Ứng viên markdown** | Thay hard-code 15%/25%; `RuleCode` = `markdown_policy` |

CTA:

| CTA | Đường dẫn |
|-----|-----------|
| **Áp dụng giảm giá** | `/product-variants?...&markdownBeforeVat=` |
| **Xem SKU** | Cùng màn, prefilled search |
| **Soạn chuyển kho** | Giữ nguyên khi có kho clearance |

Thẻ compact hiển thị tối đa **2 nút primary** (ví dụ chuyển kho + giảm giá).

---

## API

| Method | Path | Mô tả |
|--------|------|--------|
| `GET` | `/api/admin/markdown-policies` | Danh sách |
| `GET` | `/api/admin/markdown-policies/{id}` | Chi tiết |
| `POST` | `/api/admin/markdown-policies` | Tạo |
| `PUT` | `/api/admin/markdown-policies/{id}` | Cập nhật |

Quyền: system admin (giống transfer policies).

---

## Seed demo

Seed F&B (`Seed:Fb:Enabled`) tạo policy cho **Domino's** và **Popeyes** với 3 tier và sàn margin 15% / 12%.

---

## Tài liệu liên quan

| File | Nội dung |
|------|----------|
| [MarkdownPolicy.md](MarkdownPolicy.md) | English |
| [Insights.vi.md](Insights.vi.md) | Tab Insights |
| [BusinessRules.vi.md](BusinessRules.vi.md) | BR1701–BR1706 |
| [Entities.vi.md](Entities.vi.md) | Entity `MarkdownPolicy` |
