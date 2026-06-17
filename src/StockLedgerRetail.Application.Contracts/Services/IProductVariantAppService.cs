using StockLedgerRetail.ProductVariants;

namespace StockLedgerRetail.Services;

public interface IProductVariantAppService
{
    Task<List<ProductVariantDto>> GetListAsync(CancellationToken cancellationToken = default);

    Task<List<ProductVariantDto>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<ProductVariantDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProductVariantDto> CreateAsync(CreateProductVariantDto input, CancellationToken cancellationToken = default);

    Task<ProductVariantDto> UpdateAsync(Guid id, UpdateProductVariantDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
