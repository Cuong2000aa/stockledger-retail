using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Entities;

public class StockTransaction
{
    public Guid Id { get; set; }

    public string TransactionNo { get; set; } = string.Empty;

    public Guid DocumentId { get; set; }

    public Guid DocumentLineId { get; set; }

    public Guid ProductVariantId { get; set; }

    public Guid WarehouseId { get; set; }

    public StockTransactionType TransactionType { get; set; }

    public decimal QuantityDelta { get; set; }

    public decimal BeforeQuantity { get; set; }

    public decimal AfterQuantity { get; set; }

    public DateTime TransactionDate { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public InventoryDocument Document { get; set; } = null!;

    public InventoryDocumentLine DocumentLine { get; set; } = null!;

    public ProductVariant ProductVariant { get; set; } = null!;

    public Warehouse Warehouse { get; set; } = null!;

    public ICollection<CurrentStock> CurrentStocks { get; set; } = new List<CurrentStock>();
}
