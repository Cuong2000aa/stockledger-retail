using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class CurrentStockRepository : ICurrentStockRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public CurrentStockRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CurrentStock?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CurrentStocks
            .Include(x => x.ProductVariant)
            .Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<CurrentStock?> GetByVariantAndWarehouseAsync(
        Guid productVariantId,
        Guid warehouseId,
        CancellationToken cancellationToken = default) =>
        _dbContext.CurrentStocks.FirstOrDefaultAsync(
            x => x.ProductVariantId == productVariantId && x.WarehouseId == warehouseId,
            cancellationToken);

    public Task<List<CurrentStock>> GetListAsync(
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CurrentStocks
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

        return query.OrderBy(x => x.Warehouse.Code).ThenBy(x => x.ProductVariant.Sku).ToListAsync(cancellationToken);
    }

    public async Task<(List<CurrentStock> Items, int TotalCount)> GetPagedListAsync(
        Guid? warehouseId,
        Guid? productVariantId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CurrentStocks
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

        query = query.OrderBy(x => x.Warehouse.Code).ThenBy(x => x.ProductVariant.Sku);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task InsertAsync(CurrentStock currentStock, CancellationToken cancellationToken = default) =>
        await _dbContext.CurrentStocks.AddAsync(currentStock, cancellationToken);

    public Task UpdateAsync(CurrentStock currentStock, CancellationToken cancellationToken = default)
    {
        _dbContext.CurrentStocks.Update(currentStock);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
