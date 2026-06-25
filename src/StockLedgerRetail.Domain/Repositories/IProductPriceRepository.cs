using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Repositories;

public interface IProductPriceRepository
{
    Task<List<ProductPrice>> GetByVariantAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default);

    Task<ProductPrice?> GetCurrentByVariantAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default);

    Task<ProductPrice?> GetCurrentByVariantAndTypeAsync(
        Guid productVariantId,
        PriceType priceType,
        CancellationToken cancellationToken = default);

    Task InsertAsync(ProductPrice price, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProductPrice price, CancellationToken cancellationToken = default);
}
