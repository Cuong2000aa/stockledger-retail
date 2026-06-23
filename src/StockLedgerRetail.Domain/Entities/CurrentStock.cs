namespace StockLedgerRetail.Domain.Entities;

public class CurrentStock
{
    public Guid Id { get; set; }

    public Guid ProductVariantId { get; set; }

    public Guid WarehouseId { get; set; }

    public decimal QuantityOnHand { get; set; }

    public decimal QuantityReserved { get; set; }

    public decimal QuantityAvailable { get; set; }

    public Guid? LastTransactionId { get; set; }

    public DateTime LastUpdatedAt { get; set; }

    public uint RowVersion { get; set; }

    public ProductVariant ProductVariant { get; set; } = null!;

    public Warehouse Warehouse { get; set; } = null!;

    public StockTransaction? LastTransaction { get; set; }
}
