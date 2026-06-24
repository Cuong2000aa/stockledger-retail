namespace StockLedgerRetail.Application.Insights;

public static class InsightSnapshotKeyBuilder
{
    public const string KindDeadStock = "dead_stock";
    public const string KindSalesVelocity = "sales_velocity";
    public const string KindTransfer = "transfer";

    public static string BuildDeadStockKey(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        int daysWithoutOutbound,
        decimal minOnHand,
        int maxResults) =>
        string.Join('|',
            KindDeadStock,
            ScopePart("w", warehouseId),
            ScopePart("b", brandId),
            ScopePart("r", regionCode),
            $"d:{daysWithoutOutbound}",
            $"m:{minOnHand:0.####}",
            $"n:{maxResults}");

    public static string BuildSalesVelocityKey(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        int lookbackDays,
        int maxResults) =>
        string.Join('|',
            KindSalesVelocity,
            ScopePart("w", warehouseId),
            ScopePart("b", brandId),
            ScopePart("r", regionCode),
            $"l:{lookbackDays}",
            $"n:{maxResults}");

    public static string BuildTransferKey(
        Guid? sourceWarehouseId,
        Guid? destinationWarehouseId,
        Guid? brandId,
        string? regionCode,
        int lookbackDays,
        int targetCoverDays,
        int reserveCoverDays,
        int maxResults) =>
        string.Join('|',
            KindTransfer,
            ScopePart("s", sourceWarehouseId),
            ScopePart("d", destinationWarehouseId),
            ScopePart("b", brandId),
            ScopePart("r", regionCode),
            $"l:{lookbackDays}",
            $"t:{targetCoverDays}",
            $"v:{reserveCoverDays}",
            $"n:{maxResults}");

    private static string ScopePart(string prefix, Guid? value) =>
        value.HasValue ? $"{prefix}:{value:N}" : $"{prefix}:all";

    private static string ScopePart(string prefix, string? value) =>
        string.IsNullOrWhiteSpace(value) ? $"{prefix}:all" : $"{prefix}:{value.Trim().ToUpperInvariant()}";
}
