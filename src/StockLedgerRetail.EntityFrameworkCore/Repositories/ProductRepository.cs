using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public ProductRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Product?> GetByProductCodeAsync(string productCode, CancellationToken cancellationToken = default) =>
        _dbContext.Products.FirstOrDefaultAsync(x => x.ProductCode == productCode, cancellationToken);

    public Task<List<Product>> GetListAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Products.OrderBy(x => x.ProductCode).ToListAsync(cancellationToken);

    public async Task<(List<Product> Items, int TotalCount)> GetPagedListAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products.OrderBy(x => x.ProductCode);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task InsertAsync(Product product, CancellationToken cancellationToken = default) =>
        await _dbContext.Products.AddAsync(product, cancellationToken);

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _dbContext.Products.Update(product);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
    {
        _dbContext.Products.Remove(product);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
