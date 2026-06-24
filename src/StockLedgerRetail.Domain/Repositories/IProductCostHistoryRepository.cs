using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface IProductCostHistoryRepository
{
    Task<ProductCostHistory?> GetActiveByVariantAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default);

    Task<(List<ProductCostHistory> Items, int TotalCount)> GetPagedListAsync(
        Guid? productVariantId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task InsertAsync(ProductCostHistory history, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProductCostHistory history, CancellationToken cancellationToken = default);
}
