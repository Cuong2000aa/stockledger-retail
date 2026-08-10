# Thuật toán gợi ý chuyển kho (Transfer Rebalance)

Ghi chú kỹ thuật cho engine **transfer suggestions** trong Insights. Đây là chỗ “note thuật toán” — cập nhật file này khi đổi công thức / mode / trọng số.

**API:** `GET /api/inventory-insights/transfer-suggestions`  
**Rule code:** `transfer_rebalance_v2` (BR1604)  
**Code chính:**
- [`TransferRebalanceEngine.cs`](../src/StockLedgerRetail.Application/Insights/TransferRebalanceEngine.cs)
- [`TransferMinCostFlowSolver.cs`](../src/StockLedgerRetail.Application/Insights/TransferMinCostFlowSolver.cs)
- [`InsightTransferRules.cs`](../src/StockLedgerRetail.Application/Insights/InsightTransferRules.cs)
- Options: [`TransferRebalanceOptions.cs`](../src/StockLedgerRetail.Application.Contracts/Insights/TransferRebalanceOptions.cs)
- Tests: [`TransferRebalanceEngineTests.cs`](../tests/StockLedgerRetail.Application.Tests/TransferRebalanceEngineTests.cs)

Insights vẫn **chỉ đọc** (BR1601). Engine chỉ trả `TransferSuggestionDto`; tạo phiếu Draft qua `bulk-transfers` / CTA, không ghi `StockTransaction`.

---

## Luồng tổng

```
SalesVelocityFact (per SKU × warehouse)
        ↓
Tính surplus (kho thừa) / need (kho thiếu)
        ↓
Sinh cạnh hợp lệ (brand / region / TransferPolicy)
        ↓
Mode = Heuristic  ──► scored multi-pass greedy
Mode = MinCostFlow ─► successive shortest-path MCF
        ↓ (fallback Heuristic nếu timeout / quá nhiều cạnh / lỗi)
TransferSuggestionDto + InsightRecommendationEngine (CTA / priority)
```

Phân tách **theo từng SKU** (`ProductVariantId`). Không tối ưu xuyên SKU trong một lần chạy.

---

## Công thức surplus / need

Với `lookbackDays`, `targetCoverDays`, `reserveCoverDays` (API query, đã normalize):

| Đại lượng | Công thức |
|-----------|-----------|
| `avgDaily` | `OutboundQuantity / lookbackDays` |
| **Surplus** (source) | `max(0, QuantityAvailable − avgDaily × reserveCoverDays)` — nếu outbound = 0 thì surplus ≈ available |
| **Need** (dest) | `max(0, avgDaily × targetCoverDays − QuantityAvailable)` |
| **Cover days** (dest) | `QuantityAvailable / avgDaily` (null nếu avgDaily = 0) |

Dest chỉ vào hàng đợi khi `avgDaily > 0` và `need > 0`.

---

## Ràng buộc cạnh (constraint)

Cạnh `(source → dest)` chỉ tạo khi:

1. `source.WarehouseId ≠ dest.WarehouseId`
2. `InsightTransferRules.CanTransferBetweenWarehouses(...)`:
   - Region tương thích (cùng region hoặc một bên trống)
   - Product holdable tại **source** (`CanWarehouseHoldProduct`)
   - Cùng brand warehouse → dest cũng phải hold được product
   - Khác brand warehouse → cần `TransferPolicy` active `AllowCrossBrand` (BR308)

---

## Phase A — Heuristic (mặc định)

`TransferRebalanceMode.Heuristic`

1. Sinh **mọi** cạnh hợp lệ (không còn `FirstOrDefault` một source sớm).
2. Chấm điểm mỗi cạnh (với `qty ≈ min(surplus, need)` ban đầu):

```
coverGap = max(0, targetCoverDays − dest.CoverDays)   // hoặc targetCover nếu cover null

marginPerUnit = dest.SellingBeforeVat − source.CostPrice   // 0 nếu thiếu giá

marginOpportunity = marginPerUnit × qty

regionPenalty = CrossRegionPenalty nếu khác region, else 0

score = CoverGapWeight × coverGap
      + MarginWeight × marginOpportunity
      + QuantityWeight × qty
      − regionPenalty
```

3. Sắp xếp cạnh theo `score` giảm dần (tie-break: need cao hơn, cover thấp hơn).
4. Allocate một lượt: `qty = min(remainingSurplus, remainingNeed)`, cập nhật residual.
5. Gộp allocation trùng cặp `(source, dest)`, map DTO, sort toàn cục, `Take(maxResults)`.

**Severity:** `critical` nếu `dest.CoverDays < 7`, ngược lại `warning`.

### Vì sao hơn bản cũ?

Bản cũ: mỗi dest lấy `FirstOrDefault` source còn surplus (sort surplus ↓) → dễ “ăn” nguồn lớn trước, không so sánh margin / cover gap giữa các cạnh. Heuristic mới xét **toàn bộ cặp** và ưu theo score đa mục tiêu.

---

## Phase B — MinCostFlow

`TransferRebalanceMode.MinCostFlow`

Mô hình transportation **per SKU**:

| Node | Capacity / demand |
|------|-------------------|
| Super-source → sources | capacity = surplus (scale ×100 integer) |
| sources → destinations | capacity = min(surplus, need); **cost** = composite |
| destinations → super-sink | demand = need |

**Edge cost** (thấp hơn = tốt hơn):

```
cost = CoverGapWeight × dest.CoverDays
     − MarginWeight × marginPerUnit
     + regionPenalty
```

Solver: **successive shortest path** + Bellman-Ford trên residual (pure C#, không OrTools). Số lượng scale `×100` rồi chia lại khi xuất DTO.

### Fallback về Heuristic khi

- `edges.Count > MaxEdgesPerSku` (mặc định 500)
- Timeout `MinCostFlowTimeoutMs` (mặc định 2000)
- Exception trong solver

---

## Cấu hình

Section: `Inventory:TransferRebalance`

```json
{
  "Inventory": {
    "TransferRebalance": {
      "Mode": "Heuristic",
      "CoverGapWeight": 3,
      "MarginWeight": 1,
      "QuantityWeight": 0.1,
      "CrossRegionPenalty": 5,
      "MinCostFlowTimeoutMs": 2000,
      "MaxEdgesPerSku": 500
    }
  }
}
```

| Field | Mặc định | Ý nghĩa |
|-------|----------|---------|
| `Mode` | `Heuristic` | `Heuristic` \| `MinCostFlow` |
| `CoverGapWeight` | `3` | Ưu tiên dest sắp hết hàng |
| `MarginWeight` | `1` | Ưu tiên biên (sell − cost) |
| `QuantityWeight` | `0.1` | Ưu chuyến lớn hơn một chút (heuristic) |
| `CrossRegionPenalty` | `5` | Phạt khác region |
| `MinCostFlowTimeoutMs` | `2000` | Soft timeout MCF |
| `MaxEdgesPerSku` | `500` | Trần cạnh trước khi bỏ MCF |

DI: `StockLedgerRetailApplicationModule` → `ITransferRebalanceEngine` + `Configure<TransferRebalanceOptions>`.

---

## Priority CTA (`InsightRecommendationEngine`)

Sau khi có DTO, `CalculateTransferPriority` (0–100):

- Base ~55; +severity nếu severity critical; +cover thấp (&lt;7 / &lt;14); +margin opportunity lớn.

Không thay đổi thuật toán allocate — chỉ thứ tự hiển thị / CTA.

---

## Kiểm thử

`TransferRebalanceEngineTests`:

| Case | Kỳ vọng |
|------|---------|
| 1 source / 2 dest, surplus hạn chế | Dest cover thấp nhận trước |
| Cross-brand không policy | Không gợi ý |
| Cross-brand + policy | Có gợi ý |
| Residual | Tổng qty ≤ tổng surplus |
| Golden fixture | Qty cố định (18 + 2) |
| Heuristic vs MinCostFlow | Shortfall MCF ≤ heuristic; margin không kém quá 5% |
| `MaxEdgesPerSku = 0` | Fallback Heuristic vẫn ra kết quả |

Chạy:

```bash
dotnet test tests/StockLedgerRetail.Application.Tests --filter "FullyQualifiedName~TransferRebalanceEngineTests"
```

---

## Changelog thuật toán (ghi tay khi đổi)

| Ngày | Thay đổi |
|------|----------|
| 2026-08-10 | Tách `TransferRebalanceEngine`; Heuristic scored multi-pass; MinCostFlow SSP; rule `transfer_rebalance_v2`; sửa cross-brand policy (BR308) trong `InsightTransferRules` |

---

## Tài liệu liên quan

| File | Nội dung |
|------|----------|
| [TransferRebalance.md](TransferRebalance.md) | English version |
| [Insights.vi.md](Insights.vi.md) | Tổng quan Insights |
| [BusinessRules.vi.md](BusinessRules.vi.md) | BR1604, BR308 |
| [MarkdownPolicy.vi.md](MarkdownPolicy.vi.md) | Engine markdown (rule tier — khác transfer) |
| [MultiBrand.vi.md](MultiBrand.vi.md) | Brand / region / TransferPolicy |
