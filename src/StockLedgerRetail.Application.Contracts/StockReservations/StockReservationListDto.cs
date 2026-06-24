using StockLedgerRetail.Enums;

namespace StockLedgerRetail.StockReservations;

public class StockReservationListLineDto
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
}

public class StockReservationListItemDto
{
    public Guid Id { get; set; }

    public string ReservationNo { get; set; } = string.Empty;

    public string SourceSystem { get; set; } = string.Empty;

    public StockReservationReferenceType ReferenceType { get; set; }

    public string ReferenceKey { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public StockReservationStatus Status { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal TotalQuantity { get; set; }

    public List<StockReservationListLineDto> Lines { get; set; } = new();
}
