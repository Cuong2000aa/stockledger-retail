using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Audit;

public interface ITransactionAuditService
{
    Task LogAsync(
        string entityName,
        Guid entityId,
        AuditActionType action,
        object? oldValue,
        object? newValue,
        CancellationToken cancellationToken = default);
}
