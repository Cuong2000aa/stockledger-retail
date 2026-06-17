using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Entities;

public class ProductVariant : AuditedEntity
{
    public Guid ProductId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string? Barcode { get; set; }

    public string? Color { get; set; }

    public string? Size { get; set; }

    public string? Season { get; set; }

    public string? Unit { get; set; }

    public ProductStatus Status { get; set; } = ProductStatus.Active;

    public Product Product { get; set; } = null!;

    public ICollection<CurrentStock> CurrentStocks { get; set; } = new List<CurrentStock>();

    public ICollection<InventoryDocumentLine> InventoryDocumentLines { get; set; } = new List<InventoryDocumentLine>();

    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}
