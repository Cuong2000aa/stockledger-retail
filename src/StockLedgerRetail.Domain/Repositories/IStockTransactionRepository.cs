using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface IStockTransactionRepository
{
    Task InsertAsync(StockTransaction transaction, CancellationToken cancellationToken = default);

    Task<int> CountByDatePrefixAsync(string datePrefix, CancellationToken cancellationToken = default);

    Task<List<StockTransaction>> GetListAsync(
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
