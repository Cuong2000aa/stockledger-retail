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

    public decimal? CurrentSellingPriceBeforeVat { get; set; }

    public decimal? CurrentSellingPriceAfterVat { get; set; }

    public decimal? VatRate { get; set; }

    public decimal? InventoryValue { get; set; }

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

    public decimal? CurrentSellingPriceBeforeVat { get; set; }

    public decimal? CurrentSellingPriceAfterVat { get; set; }

    public decimal? VatRate { get; set; }

    public decimal? InventoryValue { get; set; }

    public DateTime? LastOutboundAt { get; set; }
}

public class MarkdownCandidateFact
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

    public decimal? CurrentSellingPriceBeforeVat { get; set; }

    public decimal? CurrentSellingPriceAfterVat { get; set; }

    public decimal? VatRate { get; set; }

    public decimal? InventoryValue { get; set; }

    public DateTime? LastOutboundAt { get; set; }
}

public class PromotionRiskFact
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

    public decimal? RegularPriceBeforeVat { get; set; }

    public decimal? RegularPriceAfterVat { get; set; }

    public decimal? PromotionPriceBeforeVat { get; set; }

    public decimal? PromotionPriceAfterVat { get; set; }

    public decimal? VatRate { get; set; }

    public DateTime? LastOutboundAt { get; set; }
}

public class ReorderRiskFact
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

    public decimal QuantityOnOrder { get; set; }

    public decimal QuantityInReceiving { get; set; }

    public decimal? CostPrice { get; set; }

    public decimal? CurrentSellingPriceAfterVat { get; set; }

    public DateTime? LastOutboundAt { get; set; }
}

public class TrendSummaryFact
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public Guid? BrandId { get; set; }

    public string? RegionCode { get; set; }

    public WarehouseType WarehouseType { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public string WarehouseName { get; set; } = string.Empty;

    public decimal CurrentQuantityOnHand { get; set; }

    public decimal CurrentInventoryValue { get; set; }

    public decimal PreviousInventoryValue { get; set; }

    public decimal CurrentOutboundQuantity { get; set; }

    public decimal PreviousOutboundQuantity { get; set; }

    public decimal? CurrentSellingPriceAfterVat { get; set; }

    public decimal? PreviousSellingPriceAfterVat { get; set; }
}
