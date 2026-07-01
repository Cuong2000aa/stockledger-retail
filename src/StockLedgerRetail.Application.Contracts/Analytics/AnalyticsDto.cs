namespace StockLedgerRetail.Analytics;

public class InventorySummaryDto
{
    public int TotalSkus { get; set; }

    public decimal TotalOnHand { get; set; }

    public decimal TotalAvailable { get; set; }

    public int WarehouseCount { get; set; }

    public int OpenPurchaseOrders { get; set; }

    public int PendingGoodsReceipts { get; set; }
}

public class StockByWarehouseDto
{
    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public string WarehouseName { get; set; } = string.Empty;

    public int SkuCount { get; set; }

    public decimal TotalOnHand { get; set; }

    public decimal TotalAvailable { get; set; }
}

public class MovementSummaryDto
{
    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    /// <summary>Operational inbound (purchase, returns, adjustments) — excludes internal transfers.</summary>
    public decimal TotalIn { get; set; }

    /// <summary>Operational outbound (sales, issues, adjustments) — excludes internal transfers.</summary>
    public decimal TotalOut { get; set; }

    /// <summary>Internal transfer inbound quantity (informational).</summary>
    public decimal TransferIn { get; set; }

    /// <summary>Internal transfer outbound quantity (informational).</summary>
    public decimal TransferOut { get; set; }

    /// <summary>Count of operational in/out transactions in the period.</summary>
    public int TransactionCount { get; set; }
}

public class LowStockItemDto
{
    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public decimal QuantityOnHand { get; set; }

    public decimal QuantityAvailable { get; set; }
}
