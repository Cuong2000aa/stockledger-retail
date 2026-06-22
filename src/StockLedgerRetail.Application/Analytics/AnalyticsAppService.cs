using StockLedgerRetail.Analytics;
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

    public AnalyticsAppService(
        ICurrentStockRepository currentStockRepository,
        IStockTransactionRepository stockTransactionRepository,
        IWarehouseRepository warehouseRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IGoodsReceiptRepository goodsReceiptRepository)
    {
        _currentStockRepository = currentStockRepository;
        _stockTransactionRepository = stockTransactionRepository;
        _warehouseRepository = warehouseRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _goodsReceiptRepository = goodsReceiptRepository;
    }

    public async Task<InventorySummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var stocks = await _currentStockRepository.GetListAsync(cancellationToken: cancellationToken);
        var warehouses = await _warehouseRepository.GetListAsync(cancellationToken);
        var (_, openPoCount) = await _purchaseOrderRepository.GetPagedListAsync(
            PurchaseOrderStatus.Submitted, null, 0, 1, null, cancellationToken);
        var (_, partialPoCount) = await _purchaseOrderRepository.GetPagedListAsync(
            PurchaseOrderStatus.PartiallyReceived, null, 0, 1, null, cancellationToken);
        var (_, pendingGrCount) = await _goodsReceiptRepository.GetPagedListAsync(
            null, GoodsReceiptStatus.Draft, 0, 1, cancellationToken);

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
        var stocks = await _currentStockRepository.GetListAsync(cancellationToken: cancellationToken);

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
        var to = toDate ?? DateTime.UtcNow;
        var from = fromDate ?? to.AddDays(-30);

        var transactions = await _stockTransactionRepository.GetByDateRangeAsync(from, to, cancellationToken);

        return new MovementSummaryDto
        {
            FromDate = from,
            ToDate = to,
            TotalIn = transactions.Where(t => t.QuantityDelta > 0).Sum(t => t.QuantityDelta),
            TotalOut = Math.Abs(transactions.Where(t => t.QuantityDelta < 0).Sum(t => t.QuantityDelta)),
            TransactionCount = transactions.Count
        };
    }

    public async Task<List<LowStockItemDto>> GetLowStockAsync(
        decimal threshold = 10,
        CancellationToken cancellationToken = default)
    {
        var stocks = await _currentStockRepository.GetListAsync(cancellationToken: cancellationToken);

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
