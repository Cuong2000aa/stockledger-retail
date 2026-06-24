using Microsoft.Extensions.Logging;
using StockLedgerRetail.Caching;

namespace StockLedgerRetail.Application.Caching;

/// <summary>
/// Xóa cache báo cáo sau khi tồn thay đổi — tránh hiển thị số liệu cũ.
/// Gọi từ StockLedgerService sau mỗi biến động tồn.
/// </summary>
public class InventoryCacheInvalidator : IInventoryCacheInvalidator
{
    private const string LogScope = "InventoryReports";

    private readonly ICacheService _cacheService;
    private readonly ILogger<InventoryCacheInvalidator> _logger;

    public InventoryCacheInvalidator(
        ICacheService cacheService,
        ILogger<InventoryCacheInvalidator> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public Task InvalidateStockAsync(
        Guid warehouseId,
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Stock changed — invalidating report cache. warehouseId={WarehouseId}, productVariantId={ProductVariantId}",
            warehouseId,
            productVariantId);

        return InvalidateWarehouseReportsAsync(warehouseId, cancellationToken);
    }

    public async Task InvalidateWarehouseReportsAsync(
        Guid? warehouseId,
        CancellationToken cancellationToken = default)
    {
        await _cacheService.RemoveByPrefixAsync(CacheKeys.ReportNxtPrefix, cancellationToken);
        await _cacheService.RemoveByPrefixAsync(CacheKeys.ReportInventoryValuePrefix, cancellationToken);

        _logger.LogInformation(
            "Report cache cleared after inventory change. scope={Scope}, warehouseId={WarehouseId}, prefixes=[{NxtPrefix}, {ValuePrefix}]",
            LogScope,
            warehouseId,
            CacheKeys.ReportNxtPrefix,
            CacheKeys.ReportInventoryValuePrefix);
    }
}
