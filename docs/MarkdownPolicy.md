# Markdown Policy (Per-brand discount rules)

Configurable **markdown / clearance pricing** rules scoped by **brand**. The policy engine drives **Insights** suggestions (dead stock, markdown candidates) and supplies **Apply markdown** CTAs that deep-link to SKU price editing.

**Admin UI:** `/[locale]/admin/markdown-policies`  
**API:** `/api/admin/markdown-policies`

---

## Purpose

Retail operators need consistent answers to:

- How long can stock sit before we suggest a discount?
- Is the **store** selling this brand well this month vs peers?
- What discount keeps **gross margin** above a brand floor?
- Who configures different rules for Domino's vs fashion brands?

`MarkdownPolicy` centralizes those answers. Insights remain **read-only** for stock; applying a price creates a `ProductPrice` row (`PriceType.Markdown`) via the existing SKU UI.

---

## Data model

| Field | Description |
|-------|-------------|
| `BrandId` | Required — one default policy per brand (optional `RegionCode` / `WarehouseType` overrides) |
| `LookbackDays` | Window for shop outbound / sell-through (default 30) |
| `MinDaysWithoutOutbound` | Minimum idle days before markdown is considered |
| `MinOnHand` | Minimum quantity on hand |
| `MinInventoryValueAtCost` | Optional — skip low-value lines |
| `MinGrossMarginPercent` | Floor margin after markdown |
| `MaxMarkdownPercent` | Hard cap on suggested discount |
| `AllowBelowCost` | Allow clearance below cost |
| `RequireApprovalAbovePercent` | Flag suggestions needing approval (UI hint) |
| `SlowSellThroughThreshold` | Ratio vs brand median sell-through; below → deeper tier % |
| `TiersJson` | JSON array of tiers (see below) |

### Tier (`MarkdownPolicyTier`)

| Field | Description |
|-------|-------------|
| `TierCode` | e.g. `watch`, `moderate`, `aggressive` |
| `MinDaysWithoutOutbound` | Tier starts at this idle age |
| `MaxDaysWithoutOutbound` | Optional upper bound |
| `MarkdownPercent` | Base suggested discount % |
| `SlowSellThroughMarkdownPercent` | Used when shop sell-through &lt; threshold × brand median |
| `Severity` | `warning` or `critical` for Insights UI |

---

## Evaluation engine

`MarkdownPolicyEngine.Evaluate` inputs:

1. **Time** — `DaysWithoutOutbound`, policy `MinDaysWithoutOutbound`
2. **Shop velocity** — outbound qty in lookback ÷ on-hand → **sell-through ratio**
3. **Brand context** — median sell-through across SKUs of the same brand in scope
4. **Pricing** — regular price before VAT, cost, VAT rate
5. **Policy** — resolved by brand → most specific match (`RegionCode`, `WarehouseType`)

Algorithm (simplified):

1. Resolve active policy for brand (or system default tiers 10% / 15% / 25%).
2. Pick highest matching tier by idle days.
3. If shop sell-through &lt; `SlowSellThroughThreshold` × brand median → use slow-tier %.
4. Clamp to `MaxMarkdownPercent`.
5. Raise price to satisfy `MinGrossMarginPercent` (and cost floor unless `AllowBelowCost`).
6. Emit `MarkdownSuggestionDto` (depth %, suggested prices, tier code, approval flag).

---

## Integration with Insights

| Insight tab | Uses policy for |
|-------------|-----------------|
| **Dead stock** | Suggested markdown on rows with `dead_stock_markdown_or_transfer` / `dead_stock_critical_markdown` |
| **Markdown candidates** | Replaces hard-coded 15%/25%; `RuleCode` = `markdown_policy` |

CTAs:

| CTA | Route |
|-----|--------|
| **Apply markdown** | `/product-variants?search=&variantId=&markdownBeforeVat=&markdownAfterVat=` |
| **Review SKU** | Same page, search prefilled |
| **Draft transfer** | Unchanged when clearance warehouse exists |

Compact insight cards show up to **two primary** CTAs (e.g. transfer + apply markdown).

---

## API

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/admin/markdown-policies` | List policies |
| `GET` | `/api/admin/markdown-policies/{id}` | Detail |
| `POST` | `/api/admin/markdown-policies` | Create |
| `PUT` | `/api/admin/markdown-policies/{id}` | Update |

Requires system admin (same as transfer policies).

---

## Demo seed

F&B seed (`Seed:Fb:Enabled`) inserts policies for **Domino's** and **Popeyes** with three tiers and margin floors (15% / 12%).

---

## Related docs

| File | Content |
|------|---------|
| [MarkdownPolicy.vi.md](MarkdownPolicy.vi.md) | Vietnamese version |
| [Insights.md](Insights.md) | Insights tabs and CTAs |
| [BusinessRules.md](BusinessRules.md) | BR1701–BR1706 |
| [Entities.md](Entities.md) | `MarkdownPolicy` entity |
