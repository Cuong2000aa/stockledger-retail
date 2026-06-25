namespace StockLedgerRetail.Domain.Entities;

/// <summary>Entity master có người tạo/sửa và thời điểm tương ứng.</summary>
public abstract class AuditedEntity
{
    public Guid Id { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }
}
