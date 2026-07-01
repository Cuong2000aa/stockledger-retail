namespace StockLedgerRetail.Caching;

/// <summary>
/// Quy ước đặt tên key cache — grep log theo prefix khi debug.
/// Ví dụ: auth:user:admin@... | report:nxt:20260101:...
/// </summary>
public static class CacheKeys
{
    public const string AuthUserPrefix = "auth:user:";

    public const string MasterBrands = "master:brands";

    public const string MasterWarehousesPrefix = "master:wh:";

    public const string ReportInventoryValuePrefix = "report:value:";

    public const string ReportNxtPrefix = "report:nxt:";

    public static string AuthUser(string email) =>
        $"{AuthUserPrefix}{email.Trim().ToLowerInvariant()}";

    public static string Warehouses(int page, int pageSize, string? search, IReadOnlyCollection<Guid>? scopedWarehouseIds = null)
    {
        var scopePart = scopedWarehouseIds is null
            ? "all"
            : string.Join(",", scopedWarehouseIds.OrderBy(x => x));

        return $"{MasterWarehousesPrefix}p{page}:s{pageSize}:q{search?.Trim().ToLowerInvariant() ?? ""}:scope:{scopePart}";
    }

    public static string InventoryValue(
        Guid? warehouseId,
        Guid? brandId,
        int page,
        int pageSize,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null)
    {
        var scopePart = FormatScope(scopedWarehouseIds);
        return $"{ReportInventoryValuePrefix}{warehouseId}:{brandId}:p{page}:s{pageSize}:scope:{scopePart}";
    }

    public static string NxtReport(
        DateTime from,
        DateTime to,
        Guid? warehouseId,
        int page,
        int pageSize,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null)
    {
        var scopePart = FormatScope(scopedWarehouseIds);
        return $"{ReportNxtPrefix}{from:yyyyMMdd}:{to:yyyyMMdd}:{warehouseId}:p{page}:s{pageSize}:scope:{scopePart}";
    }

    private static string FormatScope(IReadOnlyCollection<Guid>? scopedWarehouseIds) =>
        scopedWarehouseIds is null
            ? "all"
            : string.Join(",", scopedWarehouseIds.OrderBy(x => x));
}
