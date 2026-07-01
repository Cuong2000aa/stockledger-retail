using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Repositories;

public interface ITransactionLogRepository
{
    Task InsertAsync(TransactionLog transactionLog, CancellationToken cancellationToken = default);

    Task<(List<TransactionLog> Items, int TotalCount)> GetPagedListAsync(
        string? entityName,
        Guid? entityId,
        string? createdBy,
        AuditActionType? action,
        DateTime? createdFrom,
        DateTime? createdTo,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
