using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Product?> GetByProductCodeAsync(string productCode, CancellationToken cancellationToken = default);

    Task<List<Product>> GetListAsync(CancellationToken cancellationToken = default);

    Task<(List<Product> Items, int TotalCount)> GetPagedListAsync(
        int skip,
        int take,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task InsertAsync(Product product, CancellationToken cancellationToken = default);

    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);

    Task DeleteAsync(Product product, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
