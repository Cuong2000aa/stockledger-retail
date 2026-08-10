# Transfer Rebalance Algorithm Notes

Technical notes for the **transfer suggestions** engine in Insights. Keep this file updated when formulas, modes, or weights change.

**API:** `GET /api/inventory-insights/transfer-suggestions`  
**Rule code:** `transfer_rebalance_v2` (BR1604)  
**Primary code:**
- [`TransferRebalanceEngine.cs`](../src/StockLedgerRetail.Application/Insights/TransferRebalanceEngine.cs)
- [`TransferMinCostFlowSolver.cs`](../src/StockLedgerRetail.Application/Insights/TransferMinCostFlowSolver.cs)
- [`InsightTransferRules.cs`](../src/StockLedgerRetail.Application/Insights/InsightTransferRules.cs)
- Options: [`TransferRebalanceOptions.cs`](../src/StockLedgerRetail.Application.Contracts/Insights/TransferRebalanceOptions.cs)
- Tests: [`TransferRebalanceEngineTests.cs`](../tests/StockLedgerRetail.Application.Tests/TransferRebalanceEngineTests.cs)

Insights remain **read-only** (BR1601). The engine only emits `TransferSuggestionDto`; draft documents come from `bulk-transfers` / CTAs.

---

## Pipeline

```
SalesVelocityFact (per SKU × warehouse)
        ↓
Compute surplus / need
        ↓
Valid edges (brand / region / TransferPolicy)
        ↓
Mode = Heuristic  → scored multi-pass greedy
Mode = MinCostFlow → successive shortest-path MCF
        ↓ (fallback Heuristic on timeout / too many edges / error)
TransferSuggestionDto + recommendation CTAs
```

Optimization is **per SKU** (`ProductVariantId`).

---

## Surplus / need

| Quantity | Formula |
|----------|---------|
| `avgDaily` | `OutboundQuantity / lookbackDays` |
| **Surplus** | `max(0, QuantityAvailable − avgDaily × reserveCoverDays)` |
| **Need** | `max(0, avgDaily × targetCoverDays − QuantityAvailable)` |
| **Cover days** | `QuantityAvailable / avgDaily` (null if no outbound) |

---

## Heuristic (default)

Score each valid edge:

```
score = CoverGapWeight × coverGap
      + MarginWeight × (marginPerUnit × qty)
      + QuantityWeight × qty
      − CrossRegionPenalty   // if regions differ
```

Sort by score descending; allocate `min(remainingSurplus, remainingNeed)` once; merge duplicate pairs; global sort + `maxResults`.

---

## MinCostFlow

Bipartite transportation per SKU with composite edge **cost** (lower is better):

```
cost = CoverGapWeight × dest.CoverDays
     − MarginWeight × marginPerUnit
     + regionPenalty
```

Pure C# successive shortest path (quantities scaled ×100). Falls back to Heuristic on timeout, `MaxEdgesPerSku`, or failure.

---

## Configuration

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

---

## Algorithm changelog

| Date | Change |
|------|--------|
| 2026-08-10 | Extracted `TransferRebalanceEngine`; scored multi-pass Heuristic; MinCostFlow SSP; `transfer_rebalance_v2`; cross-brand policy fix in `InsightTransferRules` |

---

## Related docs

| File | Content |
|------|---------|
| [TransferRebalance.vi.md](TransferRebalance.vi.md) | Vietnamese (canonical notes) |
| [Insights.md](Insights.md) | Insights overview |
| [BusinessRules.md](BusinessRules.md) | BR1604, BR308 |
| [MarkdownPolicy.md](MarkdownPolicy.md) | Markdown rule engine (separate) |
