using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public WarehouseRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Warehouses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Warehouse?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _dbContext.Warehouses.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

    public Task<List<Warehouse>> GetListAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Warehouses.OrderBy(x => x.Code).ToListAsync(cancellationToken);

    public async Task<(List<Warehouse> Items, int TotalCount)> GetPagedListAsync(
        int skip,
        int take,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Warehouses.AsQueryable();
        var term = TextSearchHelper.Normalize(search);
        if (term is not null)
        {
            var pattern = TextSearchHelper.ToLikePattern(term);
            query = query.Where(x =>
                EF.Functions.ILike(x.Code, pattern) ||
                EF.Functions.ILike(x.Name, pattern) ||
                (x.AddressLine != null && EF.Functions.ILike(x.AddressLine, pattern)) ||
                (x.Ward != null && EF.Functions.ILike(x.Ward, pattern)) ||
                (x.District != null && EF.Functions.ILike(x.District, pattern)) ||
                (x.Province != null && EF.Functions.ILike(x.Province, pattern)) ||
                (x.Phone != null && EF.Functions.ILike(x.Phone, pattern)) ||
                (x.ContactName != null && EF.Functions.ILike(x.ContactName, pattern)) ||
                (x.FullAddress != null && EF.Functions.ILike(x.FullAddress, pattern)));
        }

        query = query.OrderBy(x => x.Code);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task InsertAsync(Warehouse warehouse, CancellationToken cancellationToken = default) =>
        await _dbContext.Warehouses.AddAsync(warehouse, cancellationToken);

    public Task UpdateAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
    {
        _dbContext.Warehouses.Update(warehouse);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
    {
        _dbContext.Warehouses.Remove(warehouse);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
