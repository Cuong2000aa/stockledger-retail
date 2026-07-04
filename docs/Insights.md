# Inventory Insights

Read-only decision-support APIs and UI for retail inventory operations. Insights combine stock ledger signals, pricing/valuation data, and procurement pipeline context — without posting stock.

**UI route:** `/[locale]/insights` (default `vi`)

**API prefix:** `/api/inventory-insights/*`

---

## Overview

| Layer | Responsibility |
|-------|----------------|
| **Read repository** | SQL/read-model queries over `CurrentStock`, `StockTransaction`, `ProductPrice`, `InventoryValuationSnapshot`, PO/GR |
| **App service** | Maps read models to DTOs; optional `InsightSnapshot` cache |
| **Recommendation engine** | Rule-based action cards (CTAs) per insight row |
| **Frontend** | Executive summary strip + **9 analytics tabs**, filters, drill-down links, explain modal |

Insights are **not** AI-generated today. They are deterministic rules suitable for managers and a future AI Copilot layer. `POST .../explain` returns rule-based explanations (not LLM).

---

## Executive dashboard

`GET /api/inventory-insights/executive-summary`

Aggregated KPIs for the current filter scope (`warehouseId`, `brandId`, `regionCode`):

- Dead-stock SKU count and inventory value at risk
- High-velocity SKU count
- Open transfer-suggestion count
- Markdown-candidate count
- Promotion-risk and reorder-risk counts
- Trend highlights (inventory delta vs prior period)
- Broken-size-run count and season-clearance count (when tabs have loaded)

The frontend renders this as **InsightsExecutiveSummaryStrip** above the tab bar.

---

## Analytics tabs (9)

### Operations (7)

| Tab | Endpoint | Purpose |
|-----|----------|---------|
| **Dead stock** | `GET .../dead-stock` | SKUs with on-hand stock but no outbound in N days |
| **Sales velocity** | `GET .../sales-velocity` | Outbound rate and cover days in lookback window |
| **Transfer suggestions** | `GET .../transfer-suggestions` | Move stock from surplus to deficit warehouses (same brand/region) |
| **Markdown candidates** | `GET .../markdown-candidates` | Slow movers with selling price / margin context for clearance |
| **Promotion risk** | `GET .../promotion-risk` | Active or recent promotion prices vs velocity and cover |
| **Reorder risk** | `GET .../reorder-risk` | Low cover + open PO/GR pipeline signals |
| **Trend** | `GET .../trend-summary` | Period-over-period inventory and movement deltas |

### Fashion (2)

| Tab | Endpoint | Purpose |
|-----|----------|---------|
| **Broken size runs** | `GET .../broken-size-runs` | Products with gaps in size coverage (e.g. S/M/L missing M) |
| **Season clearance** | `GET .../season-clearance` | SKUs from past seasons with slow sell-through and suggested clearance price |

Common query parameters:

- `warehouseId`, `brandId`, `regionCode` — scope filters (headers `X-Brand-Id`, `X-Warehouse-Ids`, `X-Region-Code` apply when omitted)
- `lookbackDays` — velocity, promotion, reorder, trend, fashion tabs (default 30)
- `daysWithoutOutbound` — dead stock / markdown / season clearance (default 60)
- `currentSeason` — season clearance filter (optional)
- `minOnHand`, `maxResults` — row limits

**Paged dead stock:** `GET .../dead-stock/paged` — same filters plus `page`, `pageSize`.

---

## Interactive APIs (not read-only cache)

| Method | Path | Purpose |
|--------|------|---------|
| `POST` | `.../explain` | Rule-based explanation for a specific insight row (`InsightExplainModal` in UI) |
| `POST` | `.../markdown-what-if` | Simulate markdown depth / recovery without saving `ProductPrice` |
| `POST` | `.../bulk-transfers` | Create draft transfer documents from selected transfer suggestions |

`bulk-transfers` creates **Draft** inventory documents; stock is not posted until approve.

---

## Insight action tracking

`InsightActionsController` records when users click CTAs:

| Method | Path | Purpose |
|--------|------|---------|
| `POST` | `/api/insight-actions` | Record action (`insightKind`, `actionCode`, variant/warehouse IDs, payload) |
| `GET` | `/api/insight-actions/recent` | Recent actions (`limit`, optional `insightKind`) |
| `GET` | `/api/insight-actions/stats` | Aggregated stats (`lookbackDays`) |

Persisted in `insight_action_logs`.

---

## Pricing-aware fields

Enriched DTOs expose operational pricing and valuation alongside quantity signals:

| Field group | Source | Used in |
|-------------|--------|---------|
| `CurrentSellingPriceBeforeVat`, `CurrentSellingPriceAfterVat`, `VatRate` | SKU cache / `ProductPrice` | Dead stock, velocity, transfer, markdown |
| `CurrentCostPrice`, `GrossMarginPercent` | SKU cost cache | Markdown, promotion risk |
| `InventoryValueAtCost`, `InventoryValueAtSelling` | `InventoryValuationSnapshot` or cost × on-hand | Executive summary, dead stock, markdown |
| Promotion / markdown price rows | `ProductPrice` (`PriceType`) | Promotion risk, markdown candidates |

Insights **read** prices; they do not create `ProductPrice` rows or change SKU cache.

---

## Recommendation actions (CTAs)

`InsightRecommendationEngine` attaches zero or more **actions** to each insight:

| Action type | Example routes |
|-------------|----------------|
| View stock history | `/[locale]/stock-history?...` |
| Review SKU | `/[locale]/product-variants?...` |
| Open reports | `/[locale]/reports?...` |
| Draft transfer | `/[locale]/inventory-documents?type=transfer&...` |
| Draft PO | `/[locale]/purchase-orders?...` |
| Apply markdown | `/[locale]/product-variants?...` (prefilled Markdown price form) |

Suggested markdown depth and prices come from **`MarkdownPolicyEngine`** (admin config at `/admin/markdown-policies`). See [MarkdownPolicy.md](MarkdownPolicy.md).

Action codes live in `InsightActionCodes`; types in `InsightActionTypes`. The UI renders them in **RecommendationCard** with bilingual labels under `insights.recommendation.*` in `frontend/messages`.

---

## Snapshot cache & background jobs

Heavy queries may be served from `InsightSnapshot` (keyed by `InsightSnapshotKeyBuilder`).

| Job key | Purpose |
|---------|---------|
| `insight_snapshots` | Refresh insight snapshot cache |
| `insight_alerts` | Threshold alerts (dead stock count, tied capital, reorder risk) |
| `inventory_daily_rollups` | Daily KPI rollups into `inventory_daily_rollups` |

Refresh / monitor via admin operations:

- `GET /api/admin/operations`
- `POST /api/admin/operations/jobs/{jobKey}/run`

---

## Related documentation

| File | Content |
|------|---------|
| [Insights.vi.md](Insights.vi.md) | Vietnamese version |
| [UseCases.md](UseCases.md) | UC012, UC022–UC024 |
| [BusinessRules.md](BusinessRules.md) | BR16xx insight rules |
| [MultiBrand.md](MultiBrand.md) | Brand/region scoping for insights |
| [InventoryDomain.md](InventoryDomain.md) | Domain placement |
