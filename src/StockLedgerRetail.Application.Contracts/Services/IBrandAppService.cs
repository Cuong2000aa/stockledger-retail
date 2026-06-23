using StockLedgerRetail.Brands;

namespace StockLedgerRetail.Services;

public interface IBrandAppService
{
    Task<List<BrandDto>> GetListAsync(CancellationToken cancellationToken = default);

    Task<BrandDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BrandDto> CreateAsync(CreateBrandDto input, CancellationToken cancellationToken = default);

    Task<BrandDto> UpdateAsync(Guid id, UpdateBrandDto input, CancellationToken cancellationToken = default);
}
