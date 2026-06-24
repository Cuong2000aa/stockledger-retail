using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class LotStockRepository : ILotStockRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public LotStockRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<LotStock?> GetByLotAndWarehouseAsync(
        Guid stockLotId,
        Guid warehouseId,
        CancellationToken cancellationToken = default) =>
        _dbContext.LotStocks
            .FirstOrDefaultAsync(x => x.StockLotId == stockLotId && x.WarehouseId == warehouseId, cancellationToken);

    public Task<List<LotStock>> GetFefoLotsAsync(
        Guid productVariantId,
        Guid warehouseId,
        CancellationToken cancellationToken = default) =>
        _dbContext.LotStocks
            .Include(x => x.StockLot)
            .Where(x =>
                x.WarehouseId == warehouseId
                && x.QuantityOnHand > 0
                && x.StockLot!.ProductVariantId == productVariantId)
            .OrderBy(x => x.StockLot!.ExpiryDate ?? DateTime.MaxValue)
            .ThenBy(x => x.StockLot!.ReceivedAt)
            .ToListAsync(cancellationToken);

    public async Task<(List<LotStock> Items, int TotalCount)> GetPagedListAsync(
        Guid? warehouseId,
        Guid? productVariantId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.LotStocks
            .Include(x => x.StockLot)
            .ThenInclude(x => x!.ProductVariant)
            .Include(x => x.Warehouse)
            .Where(x => x.QuantityOnHand > 0)
            .AsNoTracking();

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        }

        if (productVariantId.HasValue)
        {
            query = query.Where(x => x.StockLot!.ProductVariantId == productVariantId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.StockLot!.ExpiryDate ?? DateTime.MaxValue)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task InsertAsync(LotStock lotStock, CancellationToken cancellationToken = default) =>
        await _dbContext.LotStocks.AddAsync(lotStock, cancellationToken);

    public Task UpdateAsync(LotStock lotStock, CancellationToken cancellationToken = default)
    {
        _dbContext.LotStocks.Update(lotStock);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
