namespace StockLedgerRetail.Domain.Entities;

/// <summary>Snapshot IMEI/barcode tại thời điểm giao dịch sổ cái — không phụ thuộc trạng thái hiện tại của VariantUnitBarcode.</summary>
public class StockTransactionBarcode
{
    public Guid Id { get; set; }

    public Guid StockTransactionId { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public StockTransaction StockTransaction { get; set; } = null!;
}
