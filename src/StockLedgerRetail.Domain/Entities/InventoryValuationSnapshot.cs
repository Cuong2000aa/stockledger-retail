using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Entities;

/// <summary>
/// Snapshot định giá tồn kho theo SKU / kho / ngày để tối ưu report và analytics.
/// </summary>
public class InventoryValuationSnapshot : AuditedEntity
{
    public Guid ProductVariantId { get; set; }

    public Guid WarehouseId { get; set; }

    public decimal QuantityOnHand { get; set; }

    public decimal QuantityReserved { get; set; }

    public decimal QuantityAvailable { get; set; }

    public decimal? AverageCost { get; set; }

    public decimal InventoryValue { get; set; }

    public DateTime SnapshotDate { get; set; }

    public ValuationMethod ValuationMethod { get; set; } = ValuationMethod.WeightedAverage;

    public string Currency { get; set; } = "VND";

    public ProductVariant ProductVariant { get; set; } = null!;

    public Warehouse Warehouse { get; set; } = null!;
}
