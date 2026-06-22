using StockLedgerRetail.Common;
using StockLedgerRetail.ProductVariants;

namespace StockLedgerRetail.Services;

public interface IProductVariantAppService
{
    Task<PagedResultDto<ProductVariantDto>> GetListAsync(
        int? page = null,
        int? pageSize = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<List<ProductVariantDto>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<ProductVariantDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProductVariantDto> CreateAsync(CreateProductVariantDto input, CancellationToken cancellationToken = default);

    Task<ProductVariantDto> UpdateAsync(Guid id, UpdateProductVariantDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
