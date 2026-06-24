namespace StockLedgerRetail.Domain.Entities;

/// <summary>Lô hàng theo SKU — dùng cho F&amp;B (Dominos, Popeyes) theo dõi HSD và FEFO.</summary>
public class StockLot
{
    public Guid Id { get; set; }

    public Guid ProductVariantId { get; set; }

    public string LotCode { get; set; } = string.Empty;

    public DateTime? ExpiryDate { get; set; }

    public DateTime? ManufacturedDate { get; set; }

    public DateTime ReceivedAt { get; set; }

    public ProductVariant? ProductVariant { get; set; }

    public ICollection<LotStock> LotStocks { get; set; } = new List<LotStock>();
}
