using StockLedgerRetail.Analytics;
using StockLedgerRetail.Application.Reports;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
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

    public AnalyticsAppService(
        ICurrentStockRepository currentStockRepository,
        IStockTransactionRepository stockTransactionRepository,
        IWarehouseRepository warehouseRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IGoodsReceiptRepository goodsReceiptRepository,
        IWarehouseScopeService warehouseScopeService)
    {
        _currentStockRepository = currentStockRepository;
        _stockTransactionRepository = stockTransactionRepository;
        _warehouseRepository = warehouseRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _goodsReceiptRepository = goodsReceiptRepository;
        _warehouseScopeService = warehouseScopeService;
    }

    public async Task<InventorySummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var scope = _warehouseScopeService.ResolveListScope(null);
        var stocks = await _currentStockRepository.GetListAsync(
            scope.WarehouseId,
            scopedWarehouseIds: scope.ScopedWarehouseIds,
            cancellationToken: cancellationToken);

        var scopedWarehouseIds = scope.ScopedWarehouseIds;
        var warehouses = await _warehouseRepository.GetListAsync(cancellationToken);
        if (scopedWarehouseIds is { Count: > 0 })
        {
            warehouses = warehouses.Where(x => scopedWarehouseIds.Contains(x.Id)).ToList();
        }
        else if (scope.WarehouseId.HasValue)
        {
            warehouses = warehouses.Where(x => x.Id == scope.WarehouseId.Value).ToList();
        }

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

        return new InventorySummaryDto
        {
            TotalSkus = stocks.Count,
            TotalOnHand = stocks.Sum(s => s.QuantityOnHand),
            TotalAvailable = stocks.Sum(s => s.QuantityAvailable),
            WarehouseCount = warehouses.Count,
            OpenPurchaseOrders = openPoCount + partialPoCount,
            PendingGoodsReceipts = pendingGrCount
        };
    }

    public async Task<List<StockByWarehouseDto>> GetStockByWarehouseAsync(
        CancellationToken cancellationToken = default)
    {
        var scope = _warehouseScopeService.ResolveListScope(null);
        var stocks = await _currentStockRepository.GetListAsync(
            scope.WarehouseId,
            scopedWarehouseIds: scope.ScopedWarehouseIds,
            cancellationToken: cancellationToken);

        return stocks
            .GroupBy(s => new { s.WarehouseId, s.Warehouse.Code, s.Warehouse.Name })
            .Select(g => new StockByWarehouseDto
            {
                WarehouseId = g.Key.WarehouseId,
                WarehouseCode = g.Key.Code,
                WarehouseName = g.Key.Name,
                SkuCount = g.Count(),
                TotalOnHand = g.Sum(x => x.QuantityOnHand),
                TotalAvailable = g.Sum(x => x.QuantityAvailable)
            })
            .OrderBy(x => x.WarehouseCode)
            .ToList();
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
        var stocks = await _currentStockRepository.GetListAsync(
            scope.WarehouseId,
            scopedWarehouseIds: scope.ScopedWarehouseIds,
            cancellationToken: cancellationToken);

        return stocks
            .Where(s => s.QuantityAvailable <= threshold)
            .OrderBy(s => s.QuantityAvailable)
            .ThenBy(s => s.Warehouse.Code)
            .Select(s => new LowStockItemDto
            {
                ProductVariantId = s.ProductVariantId,
                Sku = s.ProductVariant.Sku,
                WarehouseId = s.WarehouseId,
                WarehouseCode = s.Warehouse.Code,
                QuantityOnHand = s.QuantityOnHand,
                QuantityAvailable = s.QuantityAvailable
            })
            .Take(50)
            .ToList();
    }
}
