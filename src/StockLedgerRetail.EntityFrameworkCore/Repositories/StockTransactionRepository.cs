using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Inventory;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class StockTransactionRepository : IStockTransactionRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public StockTransactionRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task InsertAsync(StockTransaction transaction, CancellationToken cancellationToken = default) =>
        await _dbContext.StockTransactions.AddAsync(transaction, cancellationToken);

    public Task<int> CountByDatePrefixAsync(string datePrefix, CancellationToken cancellationToken = default) =>
        _dbContext.StockTransactions.CountAsync(x => x.TransactionNo.StartsWith(datePrefix), cancellationToken);

    public Task<List<StockTransaction>> GetListAsync(
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.StockTransactions
            .Include(x => x.ProductVariant)
            .Include(x => x.Warehouse)
            .AsQueryable();

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        }

        if (productVariantId.HasValue)
        {
            query = query.Where(x => x.ProductVariantId == productVariantId.Value);
        }

        return query
            .OrderByDescending(x => x.TransactionDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<StockTransaction> Items, int TotalCount)> GetPagedListAsync(
        Guid? warehouseId,
        Guid? productVariantId,
        int skip,
        int take,
        string? search = null,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default)
    {
        if (!warehouseId.HasValue && scopedWarehouseIds is { Count: 0 })
        {
            return ([], 0);
        }

        var query = _dbContext.StockTransactions
            .Include(x => x.Document)
            .Include(x => x.DocumentLine)
                .ThenInclude(l => l.UnitBarcodes)
            .Include(x => x.ProductVariant)
                .ThenInclude(v => v.Product)
            .Include(x => x.Warehouse)
            .Include(x => x.CounterpartWarehouse)
            .Include(x => x.Barcodes)
            .AsQueryable();

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        }
        else if (scopedWarehouseIds is { Count: > 0 })
        {
            query = query.Where(x => scopedWarehouseIds.Contains(x.WarehouseId));
        }

        if (productVariantId.HasValue)
        {
            query = query.Where(x => x.ProductVariantId == productVariantId.Value);
        }

        var term = TextSearchHelper.Normalize(search);
        if (term is not null)
        {
            var pattern = TextSearchHelper.ToLikePattern(term);
            query = query.Where(x =>
                EF.Functions.ILike(x.TransactionNo, pattern) ||
                EF.Functions.ILike(x.DocumentNo, pattern) ||
                (x.Document != null && EF.Functions.ILike(x.Document.DocumentNo, pattern)) ||
                EF.Functions.ILike(x.ProductVariant.Sku, pattern) ||
                (x.ProductVariant.Barcode != null &&
                 EF.Functions.ILike(x.ProductVariant.Barcode, pattern)) ||
                EF.Functions.ILike(x.ProductVariant.Product.ProductCode, pattern) ||
                EF.Functions.ILike(x.ProductVariant.Product.Name, pattern) ||
                x.Barcodes.Any(b => EF.Functions.ILike(b.Barcode, pattern)));
        }

        query = query
            .OrderByDescending(x => x.TransactionDate)
            .ThenByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<List<StockTransaction>> GetByDateRangeAsync(
        DateTime fromDate,
        DateTime toDateExclusive,
        Guid? warehouseId = null,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default)
    {
        if (!warehouseId.HasValue && scopedWarehouseIds is { Count: 0 })
        {
            return Task.FromResult(new List<StockTransaction>());
        }

        var query = _dbContext.StockTransactions
            .Where(x => x.TransactionDate >= fromDate && x.TransactionDate < toDateExclusive);

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        }
        else if (scopedWarehouseIds is { Count: > 0 })
        {
            query = query.Where(x => scopedWarehouseIds.Contains(x.WarehouseId));
        }

        return query.ToListAsync(cancellationToken);
    }

    public async Task<List<StockLedgerAggregate>> GetAggregatedQuantitiesAsync(
        Guid? warehouseId = null,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default)
    {
        if (!warehouseId.HasValue && scopedWarehouseIds is { Count: 0 })
        {
            return [];
        }

        var query = _dbContext.StockTransactions.AsQueryable();

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        }
        else if (scopedWarehouseIds is { Count: > 0 })
        {
            query = query.Where(x => scopedWarehouseIds.Contains(x.WarehouseId));
        }

        return await query
            .GroupBy(x => new { x.ProductVariantId, x.WarehouseId })
            .Select(g => new StockLedgerAggregate
            {
                ProductVariantId = g.Key.ProductVariantId,
                WarehouseId = g.Key.WarehouseId,
                LedgerQuantity = g.Sum(x => x.QuantityDelta)
            })
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
