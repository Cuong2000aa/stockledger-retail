using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Insights;

public class RecordInsightActionDto
{
    public string InsightKind { get; set; } = string.Empty;

    public string ActionCode { get; set; } = string.Empty;

    public InsightActionStatus ActionStatus { get; set; }

    public Guid? ProductVariantId { get; set; }

    public Guid? WarehouseId { get; set; }

    public Guid? SourceWarehouseId { get; set; }

    public Guid? DestinationWarehouseId { get; set; }

    public Dictionary<string, string>? Payload { get; set; }

    public Guid? ResultEntityId { get; set; }

    public string? ResultEntityType { get; set; }
}

public class InsightActionLogDto
{
    public Guid Id { get; set; }

    public string InsightKind { get; set; } = string.Empty;

    public string ActionCode { get; set; } = string.Empty;

    public InsightActionStatus ActionStatus { get; set; }

    public Guid? ProductVariantId { get; set; }

    public Guid? WarehouseId { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Guid? ResultEntityId { get; set; }

    public string? ResultEntityType { get; set; }
}

public class InsightActionStatsDto
{
    public int ViewedCount { get; set; }

    public int AcceptedCount { get; set; }

    public int DismissedCount { get; set; }

    public int ExecutedCount { get; set; }

    public int AlertTriggeredCount { get; set; }

    public int TotalCount { get; set; }

    public decimal ExecutionRatePercent { get; set; }
}
