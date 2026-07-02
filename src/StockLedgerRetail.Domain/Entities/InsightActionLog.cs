using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Entities;

public class InsightActionLog
{
    public Guid Id { get; set; }

    public string InsightKind { get; set; } = string.Empty;

    public string ActionCode { get; set; } = string.Empty;

    public InsightActionStatus ActionStatus { get; set; }

    public Guid? ProductVariantId { get; set; }

    public Guid? WarehouseId { get; set; }

    public Guid? SourceWarehouseId { get; set; }

    public Guid? DestinationWarehouseId { get; set; }

    public string? PayloadJson { get; set; }

    public Guid? ResultEntityId { get; set; }

    public string? ResultEntityType { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string? IpAddress { get; set; }
}
