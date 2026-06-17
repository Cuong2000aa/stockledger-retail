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
