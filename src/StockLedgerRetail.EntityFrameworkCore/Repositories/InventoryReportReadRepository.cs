using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

/// <summary>
/// Đọc báo cáo tồn bằng Entity Framework LINQ — không dùng raw SQL trong application code.
/// </summary>
public class InventoryReportReadRepository : IInventoryReportReadRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;
    private readonly ILogger<InventoryReportReadRepository> _logger;

    public InventoryReportReadRepository(
        StockLedgerRetailDbContext dbContext,
        ILogger<InventoryReportReadRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(decimal TotalValue, int TotalLineCount)> GetInventoryValueTotalsAsync(
        Guid? warehouseId,
        Guid? brandId,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "LINQ inventory value totals: warehouseId={WarehouseId}, brandId={BrandId}",
            warehouseId,
            brandId);

        var stocks = await LoadInventoryValueStockRowsAsync(warehouseId, brandId, scopedWarehouseIds, cancellationToken);
        if (stocks.Count == 0)
        {
            return (0m, 0);
        }

        var snapshotValues = await LoadLatestSnapshotValuesAsync(
            stocks.Select(x => new StockKey(x.ProductVariantId, x.WarehouseId)).Distinct().ToList(),
            cancellationToken);

        var totalValue = stocks.Sum(row => ComputeInventoryValue(row, snapshotValues));
        var totalLineCount = stocks.Count;

        _logger.LogDebug(
            "LINQ inventory value totals result: totalValue={TotalValue}, lineCount={LineCount}",
            totalValue,
            totalLineCount);

        return (totalValue, totalLineCount);
    }

    public async Task<List<InventoryValueLineReadModel>> GetInventoryValueLinesAsync(
        Guid? warehouseId,
        Guid? brandId,
        int skip,
        int take,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "LINQ inventory value lines: warehouseId={WarehouseId}, brandId={BrandId}, skip={Skip}, take={Take}",
            warehouseId,
            brandId,
            skip,
            take);

        var lines = await ProjectInventoryValueLinesAsync(warehouseId, brandId, scopedWarehouseIds, cancellationToken);
        var page = lines
            .OrderByDescending(x => x.InventoryValue)
            .Skip(skip)
            .Take(take)
            .ToList();

        _logger.LogDebug("LINQ inventory value lines returned {RowCount} row(s).", page.Count);
        return page;
    }

    public async Task<NxtReportTotalsReadModel> GetNxtTotalsAsync(
        DateTime fromInclusive,
        DateTime toExclusive,
        Guid? warehouseId,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "LINQ NXT totals: from={FromInclusive}, toExclusive={ToExclusive}, warehouseId={WarehouseId}",
            fromInclusive,
            toExclusive,
            warehouseId);

        var lines = await BuildNxtLinesAsync(fromInclusive, toExclusive, warehouseId, scopedWarehouseIds, cancellationToken);

        var totals = new NxtReportTotalsReadModel
        {
            TotalOpeningValue = lines.Sum(x => x.OpeningValue),
            TotalInValue = lines.Sum(x => x.InValue),
            TotalOutValue = lines.Sum(x => x.OutValue),
            TotalClosingValue = lines.Sum(x => x.ClosingValue),
            TotalLineCount = lines.Count
        };

        _logger.LogDebug(
            "LINQ NXT totals: lineCount={LineCount}, closingValue={ClosingValue}",
            totals.TotalLineCount,
            totals.TotalClosingValue);

        return totals;
    }

    public async Task<List<NxtMovementLineReadModel>> GetNxtLinesAsync(
        DateTime fromInclusive,
        DateTime toExclusive,
        Guid? warehouseId,
        int skip,
        int take,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "LINQ NXT lines: from={FromInclusive}, toExclusive={ToExclusive}, warehouseId={WarehouseId}, skip={Skip}, take={Take}",
            fromInclusive,
            toExclusive,
            warehouseId,
            skip,
            take);

        var lines = await BuildNxtLinesAsync(fromInclusive, toExclusive, warehouseId, scopedWarehouseIds, cancellationToken);
        var page = lines
            .OrderByDescending(x => x.ClosingValue)
            .Skip(skip)
            .Take(take)
            .ToList();

        _logger.LogDebug("LINQ NXT lines returned {RowCount} row(s).", page.Count);
        return page;
    }

    private async Task<List<InventoryValueLineReadModel>> ProjectInventoryValueLinesAsync(
        Guid? warehouseId,
        Guid? brandId,
        IReadOnlyCollection<Guid>? scopedWarehouseIds,
        CancellationToken cancellationToken)
    {
        var stocks = await LoadInventoryValueStockRowsAsync(warehouseId, brandId, scopedWarehouseIds, cancellationToken);
        if (stocks.Count == 0)
        {
            return [];
        }

        var snapshotValues = await LoadLatestSnapshotValuesAsync(
            stocks.Select(x => new StockKey(x.ProductVariantId, x.WarehouseId)).Distinct().ToList(),
            cancellationToken);

        return stocks
            .Select(row =>
            {
                var inventoryValue = ComputeInventoryValue(row, snapshotValues);
                var key = new StockKey(row.ProductVariantId, row.WarehouseId);
                snapshotValues.TryGetValue(key, out var snapshot);
                var unitCost = snapshot?.AverageCost ?? row.CurrentCostPrice ?? row.CostPrice;

                return new InventoryValueLineReadModel
                {
                    ProductVariantId = row.ProductVariantId,
                    Sku = row.Sku,
                    WarehouseId = row.WarehouseId,
                    WarehouseCode = row.WarehouseCode,
                    QuantityOnHand = row.QuantityOnHand,
                    UnitCost = unitCost,
                    InventoryValue = inventoryValue
                };
            })
            .ToList();
    }

    private async Task<List<InventoryValueStockRow>> LoadInventoryValueStockRowsAsync(
        Guid? warehouseId,
        Guid? brandId,
        IReadOnlyCollection<Guid>? scopedWarehouseIds,
        CancellationToken cancellationToken)
    {
        if (!warehouseId.HasValue && scopedWarehouseIds is { Count: 0 })
        {
            return [];
        }

        return await (
            from currentStock in FilteredCurrentStocks(warehouseId, scopedWarehouseIds)
            where currentStock.QuantityOnHand > 0
            join productVariant in _dbContext.ProductVariants.AsNoTracking()
                on currentStock.ProductVariantId equals productVariant.Id
            where !brandId.HasValue || productVariant.BrandId == brandId.Value
            join warehouse in _dbContext.Warehouses.AsNoTracking()
                on currentStock.WarehouseId equals warehouse.Id
            select new InventoryValueStockRow(
                currentStock.ProductVariantId,
                productVariant.Sku,
                currentStock.WarehouseId,
                warehouse.Code,
                currentStock.QuantityOnHand,
                productVariant.CurrentCostPrice,
                productVariant.CostPrice))
            .ToListAsync(cancellationToken);
    }

    private static decimal ComputeInventoryValue(
        InventoryValueStockRow row,
        IReadOnlyDictionary<StockKey, SnapshotValue> snapshotValues)
    {
        var key = new StockKey(row.ProductVariantId, row.WarehouseId);
        if (snapshotValues.TryGetValue(key, out var snapshot))
        {
            return snapshot.InventoryValue;
        }

        var unitCost = row.CurrentCostPrice ?? row.CostPrice ?? 0m;
        return row.QuantityOnHand * unitCost;
    }

    private async Task<List<NxtMovementLineReadModel>> BuildNxtLinesAsync(
        DateTime fromInclusive,
        DateTime toExclusive,
        Guid? warehouseId,
        IReadOnlyCollection<Guid>? scopedWarehouseIds,
        CancellationToken cancellationToken)
    {
        if (!warehouseId.HasValue && scopedWarehouseIds is { Count: 0 })
        {
            return [];
        }

        var movements = await LoadMovementAggregatesAsync(
            fromInclusive,
            toExclusive,
            warehouseId,
            scopedWarehouseIds,
            cancellationToken);

        var stockRows = await LoadClosingStockRowsAsync(warehouseId, scopedWarehouseIds, cancellationToken);
        var keys = movements.Keys
            .Union(stockRows.Values.Where(x => x.ClosingQuantity != 0).Select(x => x.Key))
            .Distinct()
            .ToList();

        if (keys.Count == 0)
        {
            return [];
        }

        var snapshotCosts = await LoadLatestSnapshotValuesAsync(keys, cancellationToken);

        return keys
            .Select(key =>
            {
                movements.TryGetValue(key, out var movement);
                stockRows.TryGetValue(key, out var stock);

                var inQuantity = movement?.InQuantity ?? 0m;
                var outQuantity = movement?.OutQuantity ?? 0m;
                var closingQuantity = stock?.ClosingQuantity ?? 0m;
                var openingQuantity = closingQuantity - inQuantity + outQuantity;
                var unitCost = ResolveUnitCost(key, stock, snapshotCosts);

                return new NxtMovementLineReadModel
                {
                    ProductVariantId = key.ProductVariantId,
                    Sku = stock?.Sku ?? string.Empty,
                    WarehouseId = key.WarehouseId,
                    WarehouseCode = stock?.WarehouseCode ?? string.Empty,
                    OpeningQuantity = openingQuantity,
                    InQuantity = inQuantity,
                    OutQuantity = outQuantity,
                    ClosingQuantity = closingQuantity,
                    UnitCost = unitCost,
                    OpeningValue = openingQuantity * (unitCost ?? 0m),
                    InValue = inQuantity * (unitCost ?? 0m),
                    OutValue = outQuantity * (unitCost ?? 0m),
                    ClosingValue = closingQuantity * (unitCost ?? 0m)
                };
            })
            .ToList();
    }

    private IQueryable<CurrentStock> FilteredCurrentStocks(
        Guid? warehouseId,
        IReadOnlyCollection<Guid>? scopedWarehouseIds)
    {
        var query = _dbContext.CurrentStocks.AsNoTracking();
        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        }
        else if (scopedWarehouseIds is { Count: > 0 })
        {
            query = query.Where(x => scopedWarehouseIds.Contains(x.WarehouseId));
        }

        return query;
    }

    private async Task<Dictionary<StockKey, MovementAggregate>> LoadMovementAggregatesAsync(
        DateTime fromInclusive,
        DateTime toExclusive,
        Guid? warehouseId,
        IReadOnlyCollection<Guid>? scopedWarehouseIds,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.StockTransactions.AsNoTracking()
            .Where(x => x.TransactionDate >= fromInclusive && x.TransactionDate < toExclusive);

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        }
        else if (scopedWarehouseIds is { Count: > 0 })
        {
            query = query.Where(x => scopedWarehouseIds.Contains(x.WarehouseId));
        }

        var rows = await query
            .GroupBy(x => new { x.ProductVariantId, x.WarehouseId })
            .Select(g => new
            {
                g.Key.ProductVariantId,
                g.Key.WarehouseId,
                InQuantity = g.Sum(x => x.QuantityDelta > 0 ? x.QuantityDelta : 0m),
                OutQuantity = g.Sum(x => x.QuantityDelta < 0 ? -x.QuantityDelta : 0m)
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            x => new StockKey(x.ProductVariantId, x.WarehouseId),
            x => new MovementAggregate(x.InQuantity, x.OutQuantity));
    }

    private async Task<Dictionary<StockKey, ClosingStockRow>> LoadClosingStockRowsAsync(
        Guid? warehouseId,
        IReadOnlyCollection<Guid>? scopedWarehouseIds,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from currentStock in FilteredCurrentStocks(warehouseId, scopedWarehouseIds)
            join productVariant in _dbContext.ProductVariants.AsNoTracking()
                on currentStock.ProductVariantId equals productVariant.Id
            join warehouse in _dbContext.Warehouses.AsNoTracking()
                on currentStock.WarehouseId equals warehouse.Id
            select new ClosingStockRow(
                new StockKey(currentStock.ProductVariantId, currentStock.WarehouseId),
                productVariant.Sku,
                warehouse.Code,
                currentStock.QuantityOnHand,
                productVariant.CurrentCostPrice,
                productVariant.CostPrice))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.Key);
    }

    private async Task<Dictionary<StockKey, SnapshotValue>> LoadLatestSnapshotValuesAsync(
        IReadOnlyCollection<StockKey> keys,
        CancellationToken cancellationToken)
    {
        if (keys.Count == 0)
        {
            return new Dictionary<StockKey, SnapshotValue>();
        }

        var variantIds = keys.Select(x => x.ProductVariantId).Distinct().ToList();
        var warehouseIds = keys.Select(x => x.WarehouseId).Distinct().ToList();
        var keySet = keys.ToHashSet();

        var snapshots = await _dbContext.InventoryValuationSnapshots
            .AsNoTracking()
            .Where(x => variantIds.Contains(x.ProductVariantId) && warehouseIds.Contains(x.WarehouseId))
            .OrderByDescending(x => x.SnapshotDate)
            .ThenByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);

        return snapshots
            .Where(x => keySet.Contains(new StockKey(x.ProductVariantId, x.WarehouseId)))
            .GroupBy(x => new StockKey(x.ProductVariantId, x.WarehouseId))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var latest = g.First();
                    return new SnapshotValue(latest.AverageCost, latest.InventoryValue);
                });
    }

    private static decimal? ResolveUnitCost(
        StockKey key,
        ClosingStockRow? stock,
        IReadOnlyDictionary<StockKey, SnapshotValue> snapshotValues)
    {
        if (snapshotValues.TryGetValue(key, out var snapshot) && snapshot.AverageCost.HasValue)
        {
            return snapshot.AverageCost;
        }

        return stock?.CurrentCostPrice ?? stock?.CostPrice;
    }

    private readonly record struct StockKey(Guid ProductVariantId, Guid WarehouseId);

    private sealed record MovementAggregate(decimal InQuantity, decimal OutQuantity);

    private sealed record SnapshotValue(decimal? AverageCost, decimal InventoryValue);

    private sealed record ClosingStockRow(
        StockKey Key,
        string Sku,
        string WarehouseCode,
        decimal ClosingQuantity,
        decimal? CurrentCostPrice,
        decimal? CostPrice);

    private sealed record InventoryValueStockRow(
        Guid ProductVariantId,
        string Sku,
        Guid WarehouseId,
        string WarehouseCode,
        decimal QuantityOnHand,
        decimal? CurrentCostPrice,
        decimal? CostPrice);
}
