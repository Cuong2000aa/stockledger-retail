namespace StockLedgerRetail.Integration;

public class ReserveStockRequestDto
{
    public string SourceSystem { get; set; } = IntegrationSourceSystems.Pos;

    public Guid WarehouseId { get; set; }

    /// <summary>POS cart/session id while customer is still shopping.</summary>
    public string? CartSessionId { get; set; }

    /// <summary>Order id when POS already created an order draft.</summary>
    public string? OrderReference { get; set; }

    /// <summary>Override default TTL (minutes). Null uses server default (30).</summary>
    public int? ExpiresInMinutes { get; set; }

    public List<SalesLineRequestDto> Lines { get; set; } = new();
}

public class StockReservationLineDto
{
    public string Sku { get; set; } = string.Empty;

    public Guid ProductVariantId { get; set; }

    public decimal Quantity { get; set; }
}

public class ReserveStockResponseDto
{
    public Guid StockReservationId { get; set; }

    public string ReservationNo { get; set; } = string.Empty;

    public string SourceSystem { get; set; } = string.Empty;

    public string ReferenceType { get; set; } = string.Empty;

    public string ReferenceKey { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsUpdated { get; set; }

    public List<StockReservationLineDto> Lines { get; set; } = new();
}

public class ReleaseStockReservationRequestDto
{
    public string SourceSystem { get; set; } = IntegrationSourceSystems.Pos;

    public Guid WarehouseId { get; set; }

    public string? CartSessionId { get; set; }

    public string? OrderReference { get; set; }
}

public class ReleaseStockReservationResponseDto
{
    public bool Released { get; set; }

    public Guid? StockReservationId { get; set; }

    public string? ReservationNo { get; set; }
}
