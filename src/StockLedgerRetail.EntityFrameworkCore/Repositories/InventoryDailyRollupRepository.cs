using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class InventoryDailyRollupRepository : IInventoryDailyRollupRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public InventoryDailyRollupRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ReplaceForDateAsync(
        DateOnly snapshotDate,
        IReadOnlyList<InventoryDailyRollup> rollups,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.InventoryDailyRollups
            .Where(x => x.SnapshotDate == snapshotDate)
            .ExecuteDeleteAsync(cancellationToken);

        if (rollups.Count == 0)
        {
            return;
        }

        await _dbContext.InventoryDailyRollups.AddRangeAsync(rollups, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<InventoryDailyRollup>> BuildRollupsForDateAsync(
        DateOnly snapshotDate,
        CancellationToken cancellationToken = default)
    {
        var outboundFromUtc = snapshotDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(-30);
        var outboundToUtc = snapshotDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var stockRows = await (
            from stock in _dbContext.CurrentStocks.AsNoTracking()
            join variant in _dbContext.ProductVariants.AsNoTracking() on stock.ProductVariantId equals variant.Id
            join product in _dbContext.Products.AsNoTracking() on variant.ProductId equals product.Id
            join warehouse in _dbContext.Warehouses.AsNoTracking() on stock.WarehouseId equals warehouse.Id
            where stock.QuantityOnHand > 0
            where warehouse.Type != WarehouseType.InTransit
            select new
            {
                BrandId = warehouse.BrandId ?? variant.BrandId ?? product.BrandId,
                stock.WarehouseId,
                warehouse.RegionCode,
                stock.QuantityOnHand,
                stock.QuantityAvailable,
                InventoryValue = (variant.CurrentCostPrice ?? variant.CostPrice ?? 0) * stock.QuantityOnHand,
                stock.ProductVariantId
            }).ToListAsync(cancellationToken);

        var outboundRows = await _dbContext.StockTransactions
            .AsNoTracking()
            .Where(x =>
                x.TransactionType == StockTransactionType.Out
                && x.TransactionDate >= outboundFromUtc
                && x.TransactionDate <= outboundToUtc)
            .GroupBy(x => new { x.ProductVariantId, x.WarehouseId })
            .Select(g => new
            {
                g.Key.ProductVariantId,
                g.Key.WarehouseId,
                OutboundQty = g.Sum(x => -x.QuantityDelta)
            })
            .ToListAsync(cancellationToken);

        var outboundMap = outboundRows.ToDictionary(
            x => (x.ProductVariantId, x.WarehouseId),
            x => x.OutboundQty);

        var generatedAtUtc = DateTime.UtcNow;
        return stockRows
            .GroupBy(x => new { x.BrandId, x.WarehouseId, x.RegionCode })
            .Select(g =>
            {
                var skuPairs = g.Select(x => (x.ProductVariantId, x.WarehouseId)).Distinct();
                var outboundQty = skuPairs.Sum(pair => outboundMap.GetValueOrDefault(pair));
                return new InventoryDailyRollup
                {
                    Id = Guid.NewGuid(),
                    SnapshotDate = snapshotDate,
                    BrandId = g.Key.BrandId,
                    WarehouseId = g.Key.WarehouseId,
                    RegionCode = g.Key.RegionCode,
                    SkuCount = g.Count(),
                    TotalOnHand = g.Sum(x => x.QuantityOnHand),
                    TotalAvailable = g.Sum(x => x.QuantityAvailable),
                    TotalInventoryValue = g.Sum(x => x.InventoryValue),
                    OutboundQty30d = outboundQty,
                    GeneratedAtUtc = generatedAtUtc
                };
            })
            .ToList();
    }
}
