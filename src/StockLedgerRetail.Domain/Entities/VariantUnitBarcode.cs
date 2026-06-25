using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Entities;

/// <summary>Mã barcode từng đơn vị — đăng ký tồn theo SKU + kho.</summary>
public class VariantUnitBarcode
{
    public Guid Id { get; set; }

    public Guid ProductVariantId { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public Guid? WarehouseId { get; set; }

    public UnitBarcodeStatus Status { get; set; } = UnitBarcodeStatus.InStock;

    public DateTime ReceivedAt { get; set; }

    public DateTime LastUpdatedAt { get; set; }

    public ProductVariant ProductVariant { get; set; } = null!;

    public Warehouse? Warehouse { get; set; }
}
