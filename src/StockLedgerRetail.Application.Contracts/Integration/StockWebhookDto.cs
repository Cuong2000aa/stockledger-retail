namespace StockLedgerRetail.Integration;

public class StockChangedEventDto
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");

    public string EventType { get; set; } = "stock.changed";

    public string Sku { get; set; } = string.Empty;

    public Guid ProductVariantId { get; set; }

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public decimal OnHandQuantity { get; set; }

    public decimal ReservedQuantity { get; set; }

    public decimal AvailableQuantity { get; set; }

    public decimal ChangeDelta { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string SourceSystem { get; set; } = string.Empty;

    public string ReferenceNo { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

public class WebhookSubscriptionDto
{
    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<string> EventTypes { get; set; } = new() { "stock.changed" };
}

public class DispatchWebhookTestResponseDto
{
    public bool Success { get; set; }

    public int DispatchedCount { get; set; }

    public string Message { get; set; } = string.Empty;
}
