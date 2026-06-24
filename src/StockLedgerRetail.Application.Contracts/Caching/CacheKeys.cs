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

    public static string Warehouses(int page, int pageSize, string? search) =>
        $"{MasterWarehousesPrefix}p{page}:s{pageSize}:q{search?.Trim().ToLowerInvariant() ?? ""}";

    public static string InventoryValue(Guid? warehouseId, Guid? brandId, int page, int pageSize) =>
        $"{ReportInventoryValuePrefix}{warehouseId}:{brandId}:p{page}:s{pageSize}";

    public static string NxtReport(DateTime from, DateTime to, Guid? warehouseId, int page, int pageSize) =>
        $"{ReportNxtPrefix}{from:yyyyMMdd}:{to:yyyyMMdd}:{warehouseId}:p{page}:s{pageSize}";
}
