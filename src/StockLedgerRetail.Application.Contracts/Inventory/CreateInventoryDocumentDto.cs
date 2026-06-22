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

public class CreateAdjustmentLineDto
{
    public Guid ProductVariantId { get; set; }

    /// <summary>Số lượng điều chỉnh có dấu: dương = tăng tồn, âm = giảm tồn.</summary>
    public decimal AdjustmentQuantity { get; set; }

    public string? Note { get; set; }
}

public class CreateAdjustmentDto
{
    public Guid WarehouseId { get; set; }

    /// <summary>Lý do điều chỉnh — bắt buộc theo BR401.</summary>
    public string Reason { get; set; } = string.Empty;

    public DateTime? DocumentDate { get; set; }

    public string? ReferenceNo { get; set; }

    public string? Note { get; set; }

    public List<CreateAdjustmentLineDto> Lines { get; set; } = new();
}
