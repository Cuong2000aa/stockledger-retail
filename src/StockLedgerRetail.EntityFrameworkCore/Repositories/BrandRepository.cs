using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class BrandRepository : IBrandRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public BrandRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Brands.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Brand?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _dbContext.Brands.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

    public Task<List<Brand>> GetListAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Brands.OrderBy(x => x.Code).ToListAsync(cancellationToken);

    public async Task InsertAsync(Brand brand, CancellationToken cancellationToken = default) =>
        await _dbContext.Brands.AddAsync(brand, cancellationToken);

    public Task UpdateAsync(Brand brand, CancellationToken cancellationToken = default)
    {
        _dbContext.Brands.Update(brand);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
