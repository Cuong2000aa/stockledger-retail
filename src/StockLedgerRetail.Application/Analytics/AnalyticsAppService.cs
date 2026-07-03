using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Analytics;
using StockLedgerRetail.Application.Reports;
using StockLedgerRetail.Caching;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Analytics;

/// <summary>Dịch vụ phân tích tồn kho — tổng hợp số liệu read-only.</summary>
public class AnalyticsAppService : IAnalyticsAppService
{
    private readonly ICurrentStockRepository _currentStockRepository;
    private readonly IStockTransactionRepository _stockTransactionRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IGoodsReceiptRepository _goodsReceiptRepository;
    private readonly IWarehouseScopeService _warehouseScopeService;
    private readonly ICacheService _cacheService;
    private readonly CacheOptions _cacheOptions;

    public AnalyticsAppService(
        ICurrentStockRepository currentStockRepository,
        IStockTransactionRepository stockTransactionRepository,
        IWarehouseRepository warehouseRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IGoodsReceiptRepository goodsReceiptRepository,
        IWarehouseScopeService warehouseScopeService,
        ICacheService cacheService,
        Microsoft.Extensions.Options.IOptions<CacheOptions> cacheOptions)
    {
        _currentStockRepository = currentStockRepository;
        _stockTransactionRepository = stockTransactionRepository;
        _warehouseRepository = warehouseRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _goodsReceiptRepository = goodsReceiptRepository;
        _warehouseScopeService = warehouseScopeService;
        _cacheService = cacheService;
        _cacheOptions = cacheOptions.Value;
    }

    public async Task<InventorySummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var scope = _warehouseScopeService.ResolveListScope(null);
        var cacheKey = CacheKeys.AnalyticsSummary(scope.WarehouseId, scope.ScopedWarehouseIds);
        if (_cacheOptions.Enabled)
        {
            var cached = await _cacheService.GetAsync<InventorySummaryDto>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }

        var stats = await _currentStockRepository.GetSummaryStatsAsync(
            scope.WarehouseId,
            scope.ScopedWarehouseIds,
            cancellationToken);

        var scopedWarehouseIds = scope.ScopedWarehouseIds;
        var warehouseCount = await ResolveWarehouseCountAsync(scope, cancellationToken);

        var (_, openPoCount) = await _purchaseOrderRepository.GetPagedListAsync(
            PurchaseOrderStatus.Submitted, null, 0, 1, null, scope.WarehouseId, scope.ScopedWarehouseIds, cancellationToken);
        var (_, partialPoCount) = await _purchaseOrderRepository.GetPagedListAsync(
            PurchaseOrderStatus.PartiallyReceived, null, 0, 1, null, scope.WarehouseId, scope.ScopedWarehouseIds, cancellationToken);
        var (_, pendingGrCount) = await _goodsReceiptRepository.GetPagedListAsync(
            null,
            GoodsReceiptStatus.Draft,
            0,
            1,
            scope.WarehouseId,
            scope.ScopedWarehouseIds,
            cancellationToken);

        var summary = new InventorySummaryDto
        {
            TotalSkus = stats.TotalSkus,
            TotalOnHand = stats.TotalOnHand,
            TotalAvailable = stats.TotalAvailable,
            WarehouseCount = warehouseCount,
            OpenPurchaseOrders = openPoCount + partialPoCount,
            PendingGoodsReceipts = pendingGrCount
        };

        if (_cacheOptions.Enabled)
        {
            await _cacheService.SetAsync(
                cacheKey,
                summary,
                TimeSpan.FromMinutes(_cacheOptions.ReportCurrentPeriodTtlMinutes),
                cancellationToken);
        }

        return summary;
    }

    public async Task<List<StockByWarehouseDto>> GetStockByWarehouseAsync(
        CancellationToken cancellationToken = default)
    {
        var scope = _warehouseScopeService.ResolveListScope(null);
        var cacheKey = CacheKeys.AnalyticsStockByWarehouse(scope.WarehouseId, scope.ScopedWarehouseIds);
        if (_cacheOptions.Enabled)
        {
            var cached = await _cacheService.GetAsync<List<StockByWarehouseDto>>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }

        var stats = await _currentStockRepository.GetStockByWarehouseStatsAsync(
            scope.WarehouseId,
            scope.ScopedWarehouseIds,
            cancellationToken);

        var items = stats
            .Select(x => new StockByWarehouseDto
            {
                WarehouseId = x.WarehouseId,
                WarehouseCode = x.WarehouseCode,
                WarehouseName = x.WarehouseName,
                SkuCount = x.SkuCount,
                TotalOnHand = x.TotalOnHand,
                TotalAvailable = x.TotalAvailable
            })
            .ToList();

        if (_cacheOptions.Enabled)
        {
            await _cacheService.SetAsync(
                cacheKey,
                items,
                TimeSpan.FromMinutes(_cacheOptions.ReportCurrentPeriodTtlMinutes),
                cancellationToken);
        }

        return items;
    }

    public async Task<MovementSummaryDto> GetMovementSummaryAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var scope = _warehouseScopeService.ResolveListScope(null);
        var dateRange = ReportDateRange.FromOptionalUserInput(fromDate, toDate);

        var transactions = await _stockTransactionRepository.GetByDateRangeAsync(
            dateRange.FromInclusiveUtc,
            dateRange.ToExclusiveUtc,
            scope.WarehouseId,
            scope.ScopedWarehouseIds,
            cancellationToken);

        var operational = transactions.Where(t =>
            MovementMetrics.IsOperationalIn(t.TransactionType)
            || MovementMetrics.IsOperationalOut(t.TransactionType)).ToList();

        var transfers = transactions.Where(t => MovementMetrics.IsTransfer(t.TransactionType)).ToList();

        return new MovementSummaryDto
        {
            FromDate = dateRange.FromInclusiveUtc,
            ToDate = dateRange.ToDateForDisplay,
            TotalIn = operational
                .Where(t => MovementMetrics.IsOperationalIn(t.TransactionType))
                .Sum(t => t.QuantityDelta),
            TotalOut = operational
                .Where(t => MovementMetrics.IsOperationalOut(t.TransactionType))
                .Sum(t => -t.QuantityDelta),
            TransferIn = transfers
                .Where(t => t.TransactionType is StockTransactionType.TransferIn)
                .Sum(t => t.QuantityDelta),
            TransferOut = transfers
                .Where(t => t.TransactionType is StockTransactionType.TransferOut)
                .Sum(t => -t.QuantityDelta),
            TransactionCount = operational.Count
        };
    }

    public async Task<List<LowStockItemDto>> GetLowStockAsync(
        decimal threshold = 10,
        CancellationToken cancellationToken = default)
    {
        var scope = _warehouseScopeService.ResolveListScope(null);
        var items = await _currentStockRepository.GetLowStockItemsAsync(
            threshold,
            50,
            scope.WarehouseId,
            scope.ScopedWarehouseIds,
            cancellationToken);

        return items
            .Select(s => new LowStockItemDto
            {
                ProductVariantId = s.ProductVariantId,
                Sku = s.Sku,
                WarehouseId = s.WarehouseId,
                WarehouseCode = s.WarehouseCode,
                QuantityOnHand = s.QuantityOnHand,
                QuantityAvailable = s.QuantityAvailable
            })
            .ToList();
    }

    private async Task<int> ResolveWarehouseCountAsync(
        WarehouseListScope scope,
        CancellationToken cancellationToken)
    {
        if (scope.ScopedWarehouseIds is { Count: > 0 })
        {
            return scope.ScopedWarehouseIds.Count;
        }

        if (scope.WarehouseId.HasValue)
        {
            return 1;
        }

        var warehouses = await _warehouseRepository.GetListAsync(cancellationToken);
        return warehouses.Count;
    }
}
