using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Inventory;

public class StockTransactionDto
{
    public Guid Id { get; set; }

    public string TransactionNo { get; set; } = string.Empty;

    public Guid DocumentId { get; set; }

    public Guid ProductVariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;

    public StockTransactionType TransactionType { get; set; }

    public decimal QuantityDelta { get; set; }

    public decimal BeforeQuantity { get; set; }

    public decimal AfterQuantity { get; set; }

    public DateTime TransactionDate { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
