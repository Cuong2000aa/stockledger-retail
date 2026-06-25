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
        Guid? brandId,
        string? regionCode,
        DateTime referenceDateUtc,
        int daysWithoutOutbound,
        decimal minOnHand,
        int maxResults,
        CancellationToken cancellationToken = default) =>
        GetDeadStockFactsCoreAsync(
            warehouseId,
            brandId,
            regionCode,
            referenceDateUtc,
            daysWithoutOutbound,
            minOnHand,
            maxResults,
            cancellationToken);

    public Task<List<SalesVelocityFact>> GetSalesVelocityFactsAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        int maxResults,
        CancellationToken cancellationToken = default) =>
        GetSalesVelocityFactsCoreAsync(
            warehouseId,
            brandId,
            regionCode,
            fromDateUtc,
            toDateUtc,
            maxResults,
            cancellationToken);

    private async Task<List<DeadStockFact>> GetDeadStockFactsCoreAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime referenceDateUtc,
        int daysWithoutOutbound,
        decimal minOnHand,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var cutoffDate = referenceDateUtc.Date.AddDays(-daysWithoutOutbound);
        var normalizedRegion = NormalizeRegion(regionCode);

        var stocks = await (
            from stock in _dbContext.CurrentStocks.AsNoTracking()
            join productVariant in _dbContext.ProductVariants.AsNoTracking()
                on stock.ProductVariantId equals productVariant.Id
            join product in _dbContext.Products.AsNoTracking()
                on productVariant.ProductId equals product.Id
            join warehouse in _dbContext.Warehouses.AsNoTracking()
                on stock.WarehouseId equals warehouse.Id
            where stock.QuantityOnHand >= minOnHand
            where !warehouseId.HasValue || stock.WarehouseId == warehouseId.Value
            where !brandId.HasValue
                || warehouse.BrandId == brandId
                || productVariant.BrandId == brandId
                || product.BrandId == brandId
            where normalizedRegion == null
                || warehouse.RegionCode == null
                || warehouse.RegionCode.ToUpper() == normalizedRegion
            select new DeadStockFact
            {
                ProductVariantId = stock.ProductVariantId,
                Sku = productVariant.Sku,
                WarehouseId = stock.WarehouseId,
                BrandId = warehouse.BrandId ?? productVariant.BrandId ?? product.BrandId,
                RegionCode = warehouse.RegionCode,
                WarehouseType = warehouse.Type,
                WarehouseCode = warehouse.Code,
                WarehouseName = warehouse.Name,
                QuantityOnHand = stock.QuantityOnHand,
                QuantityAvailable = stock.QuantityAvailable,
                CostPrice = productVariant.CurrentCostPrice ?? productVariant.CostPrice
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
        Guid? brandId,
        string? regionCode,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var normalizedRegion = NormalizeRegion(regionCode);

        var stocks = await (
            from stock in _dbContext.CurrentStocks.AsNoTracking()
            join productVariant in _dbContext.ProductVariants.AsNoTracking()
                on stock.ProductVariantId equals productVariant.Id
            join product in _dbContext.Products.AsNoTracking()
                on productVariant.ProductId equals product.Id
            join warehouse in _dbContext.Warehouses.AsNoTracking()
                on stock.WarehouseId equals warehouse.Id
            where !warehouseId.HasValue || stock.WarehouseId == warehouseId.Value
            where !brandId.HasValue
                || warehouse.BrandId == brandId
                || productVariant.BrandId == brandId
                || product.BrandId == brandId
            where normalizedRegion == null
                || warehouse.RegionCode == null
                || warehouse.RegionCode.ToUpper() == normalizedRegion
            where warehouse.Type != WarehouseType.InTransit
            select new SalesVelocityFact
            {
                ProductVariantId = stock.ProductVariantId,
                Sku = productVariant.Sku,
                WarehouseId = stock.WarehouseId,
                BrandId = warehouse.BrandId ?? productVariant.BrandId ?? product.BrandId,
                RegionCode = warehouse.RegionCode,
                WarehouseType = warehouse.Type,
                WarehouseCode = warehouse.Code,
                WarehouseName = warehouse.Name,
                QuantityOnHand = stock.QuantityOnHand,
                QuantityAvailable = stock.QuantityAvailable,
                CostPrice = productVariant.CurrentCostPrice ?? productVariant.CostPrice,
                SellingPrice = productVariant.CurrentSellingPrice ?? productVariant.SellingPrice
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

    private static string? NormalizeRegion(string? regionCode) =>
        string.IsNullOrWhiteSpace(regionCode) ? null : regionCode.Trim().ToUpperInvariant();
}
