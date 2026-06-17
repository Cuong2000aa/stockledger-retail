namespace StockLedgerRetail.Domain.Entities;

public class InventoryDocumentLine
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public Guid ProductVariantId { get; set; }

    public decimal Quantity { get; set; }

    public decimal? UnitCost { get; set; }

    public string? Note { get; set; }

    public InventoryDocument Document { get; set; } = null!;

    public ProductVariant ProductVariant { get; set; } = null!;

    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}
