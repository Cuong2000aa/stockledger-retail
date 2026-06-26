export type InsightScopeKey = {
  warehouseId?: string;
  brandId?: string;
};

const scopeKey = (scope: InsightScopeKey = {}) =>
  [scope.warehouseId ?? "all", scope.brandId ?? "all"] as const;

export const insightQueryKeys = {
  deadStock: (scope: InsightScopeKey = {}, daysWithoutOutbound = 60) =>
    ["insights", "dead-stock", ...scopeKey(scope), daysWithoutOutbound] as const,
  salesVelocity: (scope: InsightScopeKey = {}, lookbackDays = 30) =>
    ["insights", "sales-velocity", ...scopeKey(scope), lookbackDays] as const,
  transferSuggestions: (
    sourceWarehouseId?: string,
    destinationWarehouseId?: string,
    lookbackDays = 30,
    brandId?: string
  ) =>
    [
      "insights",
      "transfer-suggestions",
      sourceWarehouseId ?? "all",
      destinationWarehouseId ?? "all",
      brandId ?? "all",
      lookbackDays,
    ] as const,
  markdownCandidates: (scope: InsightScopeKey = {}, daysWithoutOutbound = 60) =>
    ["insights", "markdown-candidates", ...scopeKey(scope), daysWithoutOutbound] as const,
  promotionRisk: (scope: InsightScopeKey = {}, lookbackDays = 30) =>
    ["insights", "promotion-risk", ...scopeKey(scope), lookbackDays] as const,
  reorderRisk: (scope: InsightScopeKey = {}, lookbackDays = 30) =>
    ["insights", "reorder-risk", ...scopeKey(scope), lookbackDays] as const,
  trendSummary: (scope: InsightScopeKey = {}, lookbackDays = 30) =>
    ["insights", "trend-summary", ...scopeKey(scope), lookbackDays] as const,
  executiveSummary: (
    scope: InsightScopeKey = {},
    lookbackDays = 30,
    daysWithoutOutbound = 60
  ) =>
    [
      "insights",
      "executive-summary",
      ...scopeKey(scope),
      lookbackDays,
      daysWithoutOutbound,
    ] as const,
};
