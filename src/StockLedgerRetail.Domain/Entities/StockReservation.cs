using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Entities;

public class StockReservation
{
    public Guid Id { get; set; }

    public string ReservationNo { get; set; } = string.Empty;

    public string SourceSystem { get; set; } = string.Empty;

    public StockReservationReferenceType ReferenceType { get; set; }

    public string ReferenceKey { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public StockReservationStatus Status { get; set; } = StockReservationStatus.Active;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? CommittedAt { get; set; }

    public DateTime? ReleasedAt { get; set; }

    public Warehouse? Warehouse { get; set; }

    public ICollection<StockReservationLine> Lines { get; set; } = new List<StockReservationLine>();
}
