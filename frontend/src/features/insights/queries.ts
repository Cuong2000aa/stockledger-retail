export const insightQueryKeys = {
  deadStock: (warehouseId?: string, daysWithoutOutbound = 60) =>
    ["insights", "dead-stock", warehouseId ?? "all", daysWithoutOutbound] as const,
  salesVelocity: (warehouseId?: string, lookbackDays = 30) =>
    ["insights", "sales-velocity", warehouseId ?? "all", lookbackDays] as const,
  transferSuggestions: (
    sourceWarehouseId?: string,
    destinationWarehouseId?: string,
    lookbackDays = 30
  ) =>
    [
      "insights",
      "transfer-suggestions",
      sourceWarehouseId ?? "all",
      destinationWarehouseId ?? "all",
      lookbackDays,
    ] as const,
  markdownCandidates: (warehouseId?: string, daysWithoutOutbound = 60) =>
    ["insights", "markdown-candidates", warehouseId ?? "all", daysWithoutOutbound] as const,
  promotionRisk: (warehouseId?: string, lookbackDays = 30) =>
    ["insights", "promotion-risk", warehouseId ?? "all", lookbackDays] as const,
  reorderRisk: (warehouseId?: string, lookbackDays = 30) =>
    ["insights", "reorder-risk", warehouseId ?? "all", lookbackDays] as const,
  trendSummary: (warehouseId?: string, lookbackDays = 30) =>
    ["insights", "trend-summary", warehouseId ?? "all", lookbackDays] as const,
  executiveSummary: (warehouseId?: string, lookbackDays = 30, daysWithoutOutbound = 60) =>
    ["insights", "executive-summary", warehouseId ?? "all", lookbackDays, daysWithoutOutbound] as const,
};
