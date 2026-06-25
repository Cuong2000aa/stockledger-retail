namespace StockLedgerRetail.Domain.Entities;

public class GoodsReceiptLineBarcode
{
    public Guid Id { get; set; }

    public Guid GoodsReceiptLineId { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public GoodsReceiptLine GoodsReceiptLine { get; set; } = null!;
}
