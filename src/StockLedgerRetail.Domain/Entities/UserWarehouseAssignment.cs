namespace StockLedgerRetail.Domain.Entities;

public class UserWarehouseAssignment
{
    public Guid UserId { get; set; }

    public Guid WarehouseId { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime AssignedAt { get; set; }

    public AppUser User { get; set; } = null!;

    public Warehouse Warehouse { get; set; } = null!;
}
