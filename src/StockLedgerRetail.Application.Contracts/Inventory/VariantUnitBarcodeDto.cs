using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Inventory;

public class VariantUnitBarcodeDto
{
    public Guid Id { get; set; }

    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public Guid? WarehouseId { get; set; }

    public string? WarehouseCode { get; set; }

    public string? WarehouseName { get; set; }

    public UnitBarcodeStatus Status { get; set; }

    public DateTime ReceivedAt { get; set; }

    public DateTime LastUpdatedAt { get; set; }
}
