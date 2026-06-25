using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Entities;

public class ProductVariant : AuditedEntity
{
    public Guid ProductId { get; set; }

    public Guid? BrandId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string? Barcode { get; set; }

    public string? Color { get; set; }

    public string? Size { get; set; }

    public string? Season { get; set; }

    public string? Unit { get; set; }

    public ProductStatus Status { get; set; } = ProductStatus.Active;

    /// <summary>Giá vốn hiện tại — có thể đồng bộ từ ERP, POS, Purchase System hoặc nhập thủ công.</summary>
    public decimal? CostPrice { get; set; }

    /// <summary>Giá bán hiện tại — dùng cho phân tích định giá tồn kho tương lai.</summary>
    public decimal? SellingPrice { get; set; }

    /// <summary>Nguồn giá vốn đang áp dụng cho <see cref="CostPrice"/>.</summary>
    public CostSource? CostSource { get; set; }

    /// <summary>Bật theo dõi lô/HSD — bắt buộc cho F&amp;B (Dominos, Popeyes).</summary>
    public bool TrackLotExpiry { get; set; }

    /// <summary>Bật quản lý barcode từng đơn vị — số lượng dòng phải bằng số mã barcode riêng biệt.</summary>
    public bool IsBarcode { get; set; }

    public Product Product { get; set; } = null!;

    public ICollection<CurrentStock> CurrentStocks { get; set; } = new List<CurrentStock>();

    public ICollection<InventoryDocumentLine> InventoryDocumentLines { get; set; } = new List<InventoryDocumentLine>();

    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();

    /// <summary>Lịch sử giá vốn theo thời gian — chuẩn bị cho tích hợp và phân tích tồn kho.</summary>
    public ICollection<ProductCostHistory> CostHistory { get; set; } = new List<ProductCostHistory>();
}
