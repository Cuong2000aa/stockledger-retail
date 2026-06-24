using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Reports;

public class InventoryValueLineDto
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public decimal QuantityOnHand { get; set; }

    public decimal? UnitCost { get; set; }

    public decimal InventoryValue { get; set; }
}

public class InventoryValueReportDto
{
    public decimal TotalValue { get; set; }

    public int TotalLineCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public List<InventoryValueLineDto> Lines { get; set; } = new();
}

public class NxtMovementLineDto
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public decimal OpeningQuantity { get; set; }

    public decimal InQuantity { get; set; }

    public decimal OutQuantity { get; set; }

    public decimal ClosingQuantity { get; set; }

    public decimal? UnitCost { get; set; }

    public decimal OpeningValue { get; set; }

    public decimal InValue { get; set; }

    public decimal OutValue { get; set; }

    public decimal ClosingValue { get; set; }
}

public class NxtReportDto
{
    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public decimal TotalOpeningValue { get; set; }

    public decimal TotalInValue { get; set; }

    public decimal TotalOutValue { get; set; }

    public decimal TotalClosingValue { get; set; }

    public int TotalLineCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public List<NxtMovementLineDto> Lines { get; set; } = new();
}

public class ProductCostHistoryDto
{
    public Guid Id { get; set; }

    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public decimal CostPrice { get; set; }

    public CostSource CostSource { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }
}

public class NearExpiryLotDto
{
    public Guid StockLotId { get; set; }

    public string LotCode { get; set; } = string.Empty;

    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public decimal QuantityOnHand { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public int DaysUntilExpiry { get; set; }
}

public class LotStockDto
{
    public Guid Id { get; set; }

    public Guid StockLotId { get; set; }

    public string LotCode { get; set; } = string.Empty;

    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public decimal QuantityOnHand { get; set; }

    public DateTime? ExpiryDate { get; set; }
}
