using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface IProductVariantRepository
{
    Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProductVariant?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);

    Task<ProductVariant?> GetByBrandIdAndSkuAsync(
        Guid? brandId,
        string sku,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, Guid?>> GetBrandIdsByVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken = default);

    Task<List<ProductVariant>> GetListAsync(CancellationToken cancellationToken = default);

    Task<(List<ProductVariant> Items, int TotalCount)> GetPagedListAsync(
        int skip,
        int take,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<List<ProductVariant>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    Task InsertAsync(ProductVariant productVariant, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProductVariant productVariant, CancellationToken cancellationToken = default);

    Task DeleteAsync(ProductVariant productVariant, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
