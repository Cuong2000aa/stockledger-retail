namespace StockLedgerRetail.Domain.Entities;

public class InventoryDailyRollup
{
    public Guid Id { get; set; }

    public DateOnly SnapshotDate { get; set; }

    public Guid? BrandId { get; set; }

    public Guid? WarehouseId { get; set; }

    public string? RegionCode { get; set; }

    public int SkuCount { get; set; }

    public decimal TotalOnHand { get; set; }

    public decimal TotalAvailable { get; set; }

    public decimal TotalInventoryValue { get; set; }

    public decimal OutboundQty30d { get; set; }

    public DateTime GeneratedAtUtc { get; set; }
}
