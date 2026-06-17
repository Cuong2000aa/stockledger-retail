namespace StockLedgerRetail.Integration;

public class SalesLineRequestDto
{
    public string Sku { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
}

public class SalesAvailabilityLineDto
{
    public string Sku { get; set; } = string.Empty;

    public Guid? ProductVariantId { get; set; }

    public decimal RequestedQuantity { get; set; }

    public decimal AvailableQuantity { get; set; }

    public bool IsAvailable { get; set; }

    public string? Message { get; set; }
}

public class CheckSalesAvailabilityRequestDto
{
    public Guid WarehouseId { get; set; }

    public List<SalesLineRequestDto> Lines { get; set; } = new();
}

public class CheckSalesAvailabilityResponseDto
{
    public Guid WarehouseId { get; set; }

    public bool CanFulfillAll { get; set; }

    public List<SalesAvailabilityLineDto> Lines { get; set; } = new();
}

public class ConfirmSaleRequestDto
{
    /// <summary>External system id, e.g. POS, OMS, ECOM.</summary>
    public string SourceSystem { get; set; } = IntegrationSourceSystems.Pos;

    /// <summary>Order id from sales system (unique per source system).</summary>
    public string OrderReference { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public DateTime? SaleDate { get; set; }

    public string? Note { get; set; }

    public List<SalesLineRequestDto> Lines { get; set; } = new();
}

public class ConfirmSaleResponseDto
{
    public bool IsReplay { get; set; }

    public Guid InventoryDocumentId { get; set; }

    public string DocumentNo { get; set; } = string.Empty;

    public string SourceSystem { get; set; } = string.Empty;

    public string OrderReference { get; set; } = string.Empty;
}

public class ConfirmReturnRequestDto
{
    public string SourceSystem { get; set; } = IntegrationSourceSystems.Pos;

  /// <summary>Return id from sales system (unique per source system).</summary>
    public string ReturnReference { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public DateTime? ReturnDate { get; set; }

    public string? Note { get; set; }

    public List<SalesLineRequestDto> Lines { get; set; } = new();
}

public class ConfirmReturnResponseDto
{
    public bool IsReplay { get; set; }

    public Guid InventoryDocumentId { get; set; }

    public string DocumentNo { get; set; } = string.Empty;

    public string SourceSystem { get; set; } = string.Empty;

    public string ReturnReference { get; set; } = string.Empty;
}
