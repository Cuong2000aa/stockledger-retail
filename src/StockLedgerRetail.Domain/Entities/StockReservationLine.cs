namespace StockLedgerRetail.Domain.Entities;

public class StockReservationLine
{
    public Guid Id { get; set; }

    public Guid StockReservationId { get; set; }

    public Guid ProductVariantId { get; set; }

    public decimal Quantity { get; set; }

    public StockReservation? StockReservation { get; set; }

    public ProductVariant? ProductVariant { get; set; }
}
