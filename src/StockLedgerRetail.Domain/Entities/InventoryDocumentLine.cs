namespace StockLedgerRetail.Domain.Entities;

public class InventoryDocumentLine
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public Guid ProductVariantId { get; set; }

    public decimal Quantity { get; set; }

    public decimal? UnitCost { get; set; }

    public Guid? StockLotId { get; set; }

    public string? LotCode { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string? Note { get; set; }

    public InventoryDocument Document { get; set; } = null!;

    public ProductVariant ProductVariant { get; set; } = null!;

    public StockLot? StockLot { get; set; }

    public ICollection<InventoryDocumentLineBarcode> UnitBarcodes { get; set; } =
        new List<InventoryDocumentLineBarcode>();

    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}
