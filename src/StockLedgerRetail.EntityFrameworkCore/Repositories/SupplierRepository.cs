using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public SupplierRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Suppliers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Supplier?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _dbContext.Suppliers.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

    public async Task<(List<Supplier> Items, int TotalCount)> GetPagedListAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Suppliers.OrderBy(x => x.Code);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task InsertAsync(Supplier supplier, CancellationToken cancellationToken = default) =>
        await _dbContext.Suppliers.AddAsync(supplier, cancellationToken);

    public Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        _dbContext.Suppliers.Update(supplier);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        _dbContext.Suppliers.Remove(supplier);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
