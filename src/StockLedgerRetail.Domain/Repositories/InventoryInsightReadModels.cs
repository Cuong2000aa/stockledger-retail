using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Repositories;

public class DeadStockFact
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public Guid? BrandId { get; set; }

    public string? RegionCode { get; set; }

    public WarehouseType WarehouseType { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public string WarehouseName { get; set; } = string.Empty;

    public decimal QuantityOnHand { get; set; }

    public decimal QuantityAvailable { get; set; }

    public decimal? CostPrice { get; set; }

    public DateTime? LastOutboundAt { get; set; }
}

public class SalesVelocityFact
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public Guid? BrandId { get; set; }

    public string? RegionCode { get; set; }

    public WarehouseType WarehouseType { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public string WarehouseName { get; set; } = string.Empty;

    public decimal QuantityOnHand { get; set; }

    public decimal QuantityAvailable { get; set; }

    public decimal OutboundQuantity { get; set; }

    public decimal? CostPrice { get; set; }

    public decimal? SellingPrice { get; set; }

    public DateTime? LastOutboundAt { get; set; }
}
