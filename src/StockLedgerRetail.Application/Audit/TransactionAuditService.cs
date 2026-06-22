using System.Text.Json;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Audit;

/// <summary>
/// Ghi log thay đổi dữ liệu (CREATE/UPDATE/DELETE/APPROVE) vào bảng TransactionLog.
/// </summary>
public class TransactionAuditService : ITransactionAuditService
{
    private readonly ITransactionLogRepository _transactionLogRepository;
    private readonly IAuditContext _auditContext;

    public TransactionAuditService(
        ITransactionLogRepository transactionLogRepository,
        IAuditContext auditContext)
    {
        _transactionLogRepository = transactionLogRepository;
        _auditContext = auditContext;
    }

    /// <summary>
    /// Ghi một bản ghi audit — lưu giá trị cũ/mới dạng JSON, kèm user và IP từ request.
    /// </summary>
    public async Task LogAsync(
        string entityName,
        Guid entityId,
        AuditActionType action,
        object? oldValue,
        object? newValue,
        CancellationToken cancellationToken = default)
    {
        var log = new TransactionLog
        {
            Id = Guid.NewGuid(),
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue),
            NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue),
            CreatedBy = _auditContext.UserName,
            CreatedAt = DateTime.UtcNow,
            IpAddress = _auditContext.IpAddress
        };

        await _transactionLogRepository.InsertAsync(log, cancellationToken);
        await _transactionLogRepository.SaveChangesAsync(cancellationToken);
    }
}
