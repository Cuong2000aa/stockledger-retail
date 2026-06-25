using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface ITransactionLogRepository
{
    Task InsertAsync(TransactionLog transactionLog, CancellationToken cancellationToken = default);

    Task<(List<TransactionLog> Items, int TotalCount)> GetPagedListAsync(
        string? entityName,
        Guid? entityId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
