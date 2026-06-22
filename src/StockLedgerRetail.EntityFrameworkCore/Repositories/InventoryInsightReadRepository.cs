using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class InventoryInsightReadRepository : IInventoryInsightReadRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public InventoryInsightReadRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<DeadStockFact>> GetDeadStockFactsAsync(
        Guid? warehouseId,
        DateTime referenceDateUtc,
        int daysWithoutOutbound,
        decimal minOnHand,
        int maxResults,
        CancellationToken cancellationToken = default) =>
        GetDeadStockFactsCoreAsync(
            warehouseId,
            referenceDateUtc,
            daysWithoutOutbound,
            minOnHand,
            maxResults,
            cancellationToken);

    public Task<List<SalesVelocityFact>> GetSalesVelocityFactsAsync(
        Guid? warehouseId,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        int maxResults,
        CancellationToken cancellationToken = default) =>
        GetSalesVelocityFactsCoreAsync(
            warehouseId,
            fromDateUtc,
            toDateUtc,
            maxResults,
            cancellationToken);

    private async Task<List<DeadStockFact>> GetDeadStockFactsCoreAsync(
        Guid? warehouseId,
        DateTime referenceDateUtc,
        int daysWithoutOutbound,
        decimal minOnHand,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var cutoffDate = referenceDateUtc.Date.AddDays(-daysWithoutOutbound);
        var stocks = await (
            from stock in _dbContext.CurrentStocks.AsNoTracking()
            join productVariant in _dbContext.ProductVariants.AsNoTracking()
                on stock.ProductVariantId equals productVariant.Id
            join warehouse in _dbContext.Warehouses.AsNoTracking()
                on stock.WarehouseId equals warehouse.Id
            where stock.QuantityOnHand >= minOnHand
            where !warehouseId.HasValue || stock.WarehouseId == warehouseId.Value
            select new DeadStockFact
            {
                ProductVariantId = stock.ProductVariantId,
                Sku = productVariant.Sku,
                WarehouseId = stock.WarehouseId,
                WarehouseCode = warehouse.Code,
                WarehouseName = warehouse.Name,
                QuantityOnHand = stock.QuantityOnHand,
                QuantityAvailable = stock.QuantityAvailable,
                CostPrice = productVariant.CostPrice
            })
            .ToListAsync(cancellationToken);

        var outboundFacts = await _dbContext.StockTransactions
            .AsNoTracking()
            .Where(x => x.TransactionType == StockTransactionType.Out)
            .GroupBy(x => new { x.ProductVariantId, x.WarehouseId })
            .Select(g => new
            {
                g.Key.ProductVariantId,
                g.Key.WarehouseId,
                LastOutboundAt = g.Max(x => (DateTime?)x.TransactionDate)
            })
            .ToDictionaryAsync(
                x => (x.ProductVariantId, x.WarehouseId),
                x => x.LastOutboundAt,
                cancellationToken);

        return stocks
            .Select(x =>
            {
                outboundFacts.TryGetValue((x.ProductVariantId, x.WarehouseId), out var lastOutboundAt);
                x.LastOutboundAt = lastOutboundAt;
                return x;
            })
            .Where(x => !x.LastOutboundAt.HasValue || x.LastOutboundAt.Value <= cutoffDate)
            .OrderBy(x => x.LastOutboundAt ?? DateTime.MinValue)
            .ThenByDescending(x => x.QuantityOnHand)
            .Take(maxResults)
            .ToList();
    }

    private async Task<List<SalesVelocityFact>> GetSalesVelocityFactsCoreAsync(
        Guid? warehouseId,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var stocks = await (
            from stock in _dbContext.CurrentStocks.AsNoTracking()
            join productVariant in _dbContext.ProductVariants.AsNoTracking()
                on stock.ProductVariantId equals productVariant.Id
            join warehouse in _dbContext.Warehouses.AsNoTracking()
                on stock.WarehouseId equals warehouse.Id
            where !warehouseId.HasValue || stock.WarehouseId == warehouseId.Value
            select new SalesVelocityFact
            {
                ProductVariantId = stock.ProductVariantId,
                Sku = productVariant.Sku,
                WarehouseId = stock.WarehouseId,
                WarehouseCode = warehouse.Code,
                WarehouseName = warehouse.Name,
                QuantityOnHand = stock.QuantityOnHand,
                QuantityAvailable = stock.QuantityAvailable,
                CostPrice = productVariant.CostPrice,
                SellingPrice = productVariant.SellingPrice
            })
            .ToListAsync(cancellationToken);

        var outboundFacts = await _dbContext.StockTransactions
            .AsNoTracking()
            .Where(x =>
                x.TransactionType == StockTransactionType.Out
                && x.TransactionDate >= fromDateUtc
                && x.TransactionDate <= toDateUtc)
            .GroupBy(x => new { x.ProductVariantId, x.WarehouseId })
            .Select(g => new
            {
                g.Key.ProductVariantId,
                g.Key.WarehouseId,
                OutboundQuantity = g.Sum(x => -x.QuantityDelta),
                LastOutboundAt = g.Max(x => (DateTime?)x.TransactionDate)
            })
            .ToDictionaryAsync(
                x => (x.ProductVariantId, x.WarehouseId),
                x => new { x.OutboundQuantity, x.LastOutboundAt },
                cancellationToken);

        return stocks
            .Select(x =>
            {
                if (outboundFacts.TryGetValue((x.ProductVariantId, x.WarehouseId), out var outbound))
                {
                    x.OutboundQuantity = outbound.OutboundQuantity;
                    x.LastOutboundAt = outbound.LastOutboundAt;
                }

                return x;
            })
            .OrderByDescending(x => x.OutboundQuantity)
            .ThenBy(x => x.QuantityAvailable)
            .Take(maxResults)
            .ToList();
    }
}
