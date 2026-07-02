namespace StockLedgerRetail.Insights;

public class BulkTransferLineRequestDto
{
    public Guid ProductVariantId { get; set; }

    public Guid SourceWarehouseId { get; set; }

    public Guid DestinationWarehouseId { get; set; }

    public decimal Quantity { get; set; }

    public string? Sku { get; set; }
}

public class BulkTransferFromInsightsRequestDto
{
    public List<BulkTransferLineRequestDto> Lines { get; set; } = new();

    public string? Note { get; set; }
}

public class BulkTransferFromInsightsResultDto
{
    public List<BulkTransferDocumentResultDto> Documents { get; set; } = new();

    public int SuccessCount { get; set; }

    public int FailureCount { get; set; }

    public List<string> Errors { get; set; } = new();
}

public class BulkTransferDocumentResultDto
{
    public Guid DocumentId { get; set; }

    public string DocumentNo { get; set; } = string.Empty;

    public Guid SourceWarehouseId { get; set; }

    public Guid DestinationWarehouseId { get; set; }

    public int LineCount { get; set; }
}
