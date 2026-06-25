using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Audit;

public class TransactionLogDto
{
    public Guid Id { get; set; }

    public string EntityName { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public AuditActionType Action { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string? IpAddress { get; set; }
}
