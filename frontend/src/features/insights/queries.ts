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
};
