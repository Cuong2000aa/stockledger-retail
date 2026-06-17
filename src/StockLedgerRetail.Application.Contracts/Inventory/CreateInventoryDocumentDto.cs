namespace StockLedgerRetail.Inventory;

public class CreateInventoryDocumentLineDto
{
    public Guid ProductVariantId { get; set; }

    public decimal Quantity { get; set; }

    public decimal? UnitCost { get; set; }

    public string? Note { get; set; }
}

public class CreateStockInDto
{
    public Guid DestinationWarehouseId { get; set; }

    public DateTime? DocumentDate { get; set; }

    public string? ReferenceNo { get; set; }

    public string? SourceSystem { get; set; }

    public string? Note { get; set; }

    public List<CreateInventoryDocumentLineDto> Lines { get; set; } = new();
}

public class CreateStockOutDto
{
    public Guid SourceWarehouseId { get; set; }

    public DateTime? DocumentDate { get; set; }

    public string? ReferenceNo { get; set; }

    public string? SourceSystem { get; set; }

    public string? Note { get; set; }

    public List<CreateInventoryDocumentLineDto> Lines { get; set; } = new();
}
