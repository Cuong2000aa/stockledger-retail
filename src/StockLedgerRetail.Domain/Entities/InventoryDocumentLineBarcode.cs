namespace StockLedgerRetail.Domain.Entities;

public class InventoryDocumentLineBarcode
{
    public Guid Id { get; set; }

    public Guid InventoryDocumentLineId { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public InventoryDocumentLine Line { get; set; } = null!;
}
