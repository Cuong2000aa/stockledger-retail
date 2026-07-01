using StockLedgerRetail.Audit;

namespace StockLedgerRetail.HttpApi.Host.Middleware;

/// <summary>
/// Đọc phạm vi brand/kho/vùng từ header (Phase 4 RBAC-lite).
/// X-Brand-Id, X-Warehouse-Ids (comma-separated GUID), X-Region-Code.
/// Giao với phạm vi kho của user đăng nhập.
/// </summary>
public class BrandScopeMiddleware
{
    public const string BrandIdHeader = "X-Brand-Id";
    public const string WarehouseIdsHeader = "X-Warehouse-Ids";
    public const string RegionCodeHeader = "X-Region-Code";

    private readonly RequestDelegate _next;

    public BrandScopeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IBrandScopeContext scopeContext,
        IUserWarehouseScopeContext userWarehouseScope)
    {
        if (scopeContext is BrandScopeContext mutable)
        {
            if (context.Request.Headers.TryGetValue(BrandIdHeader, out var brandHeader)
                && Guid.TryParse(brandHeader.ToString(), out var brandId))
            {
                mutable.BrandId = brandId;
            }

            if (context.Request.Headers.TryGetValue(WarehouseIdsHeader, out var warehouseHeader))
            {
                var ids = warehouseHeader.ToString()
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => Guid.TryParse(x, out var id) ? id : (Guid?)null)
                    .Where(x => x.HasValue)
                    .Select(x => x!.Value)
                    .ToList();

                if (ids.Count > 0)
                {
                    mutable.WarehouseIds = ids;
                }
            }

            if (context.Request.Headers.TryGetValue(RegionCodeHeader, out var regionHeader)
                && !string.IsNullOrWhiteSpace(regionHeader))
            {
                mutable.RegionCode = regionHeader.ToString().Trim();
            }

            ApplyUserWarehouseScope(mutable, userWarehouseScope);
        }

        await _next(context);
    }

    private static void ApplyUserWarehouseScope(
        BrandScopeContext brandScope,
        IUserWarehouseScopeContext userWarehouseScope)
    {
        if (userWarehouseScope.AllowedWarehouseIds is not { Count: > 0 })
        {
            return;
        }

        var allowed = userWarehouseScope.AllowedWarehouseIds;

        if (brandScope.WarehouseIds is not { Count: > 0 })
        {
            brandScope.WarehouseIds = allowed.ToList();
            return;
        }

        brandScope.WarehouseIds = brandScope.WarehouseIds
            .Intersect(allowed)
            .ToList();
    }
}
