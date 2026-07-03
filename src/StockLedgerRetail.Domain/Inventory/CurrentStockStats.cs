namespace StockLedgerRetail.Domain.Inventory;

public class CurrentStockSummaryStats
{
    public int TotalSkus { get; set; }

    public decimal TotalOnHand { get; set; }

    public decimal TotalAvailable { get; set; }
}

public class StockByWarehouseStats
{
    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public string WarehouseName { get; set; } = string.Empty;

    public int SkuCount { get; set; }

    public decimal TotalOnHand { get; set; }

    public decimal TotalAvailable { get; set; }
}

public class LowStockItemStats
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public decimal QuantityOnHand { get; set; }

    public decimal QuantityAvailable { get; set; }
}

public class StockActivityPair
{
    public Guid ProductVariantId { get; set; }

    public Guid WarehouseId { get; set; }
}
