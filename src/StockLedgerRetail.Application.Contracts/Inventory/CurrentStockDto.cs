namespace StockLedgerRetail.Inventory;

public class CurrentStockDto
{
    public Guid Id { get; set; }

    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public string WarehouseName { get; set; } = string.Empty;

    public decimal QuantityOnHand { get; set; }

    public decimal QuantityReserved { get; set; }

    public decimal QuantityAvailable { get; set; }

    public bool IsBarcode { get; set; }

    public Guid? LastTransactionId { get; set; }

    public DateTime LastUpdatedAt { get; set; }
}
