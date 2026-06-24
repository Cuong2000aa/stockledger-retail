using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class StockLotRepository : IStockLotRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public StockLotRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<StockLot?> GetByVariantAndLotCodeAsync(
        Guid productVariantId,
        string lotCode,
        CancellationToken cancellationToken = default)
    {
        var tracked = _dbContext.StockLots.Local.FirstOrDefault(
            x => x.ProductVariantId == productVariantId && x.LotCode == lotCode);
        if (tracked is not null)
        {
            return Task.FromResult<StockLot?>(tracked);
        }

        return _dbContext.StockLots
            .FirstOrDefaultAsync(
                x => x.ProductVariantId == productVariantId && x.LotCode == lotCode,
                cancellationToken);
    }

    public Task<List<StockLot>> GetNearExpiryAsync(
        DateTime expiryBefore,
        Guid? warehouseId,
        Guid? brandId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.StockLots
            .Include(x => x.ProductVariant)
            .Include(x => x.LotStocks)
            .ThenInclude(x => x.Warehouse)
            .Where(x => x.ExpiryDate != null && x.ExpiryDate <= expiryBefore)
            .Where(x => x.LotStocks.Any(ls => ls.QuantityOnHand > 0));

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.LotStocks.Any(ls => ls.WarehouseId == warehouseId.Value && ls.QuantityOnHand > 0));
        }

        if (brandId.HasValue)
        {
            query = query.Where(x => x.ProductVariant!.BrandId == brandId.Value);
        }

        return query
            .OrderBy(x => x.ExpiryDate)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<StockLot> Items, int TotalCount)> GetPagedListAsync(
        Guid? productVariantId,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.StockLots
            .Include(x => x.ProductVariant)
            .AsNoTracking();

        if (productVariantId.HasValue)
        {
            query = query.Where(x => x.ProductVariantId == productVariantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.LotCode.ToLower().Contains(term)
                || x.ProductVariant!.Sku.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.ReceivedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task InsertAsync(StockLot lot, CancellationToken cancellationToken = default) =>
        await _dbContext.StockLots.AddAsync(lot, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
