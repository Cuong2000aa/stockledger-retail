using StockLedgerRetail.Products;

namespace StockLedgerRetail.Services;

public interface IProductAppService
{
    Task<List<ProductDto>> GetListAsync(CancellationToken cancellationToken = default);

    Task<ProductDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProductDto> CreateAsync(CreateProductDto input, CancellationToken cancellationToken = default);

    Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
