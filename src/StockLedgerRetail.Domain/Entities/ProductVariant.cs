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

    /// <summary>Giá vốn hiện tại — legacy/current cache để tương thích report và luồng cũ.</summary>
    public decimal? CostPrice { get; set; }

    /// <summary>Giá bán hiện tại — legacy/current cache để tương thích report và luồng cũ.</summary>
    public decimal? SellingPrice { get; set; }

    /// <summary>Nguồn giá vốn đang áp dụng cho <see cref="CostPrice"/>.</summary>
    public CostSource? CostSource { get; set; }

    /// <summary>Current cache của giá vốn đang hiệu lực.</summary>
    public decimal? CurrentCostPrice { get; set; }

    /// <summary>Current cache của giá bán đang hiệu lực.</summary>
    public decimal? CurrentSellingPrice { get; set; }

    /// <summary>Current cache của giá bán trước VAT.</summary>
    public decimal? CurrentSellingPriceBeforeVat { get; set; }

    /// <summary>Current cache của giá bán sau VAT.</summary>
    public decimal? CurrentSellingPriceAfterVat { get; set; }

    /// <summary>Thuế VAT mặc định cho SKU (%).</summary>
    public decimal? VatRate { get; set; }

    /// <summary>Current cache của nguồn giá vốn.</summary>
    public CostSource? CurrentCostSource { get; set; }

    public DateTime? CurrentCostEffectiveFrom { get; set; }

    public DateTime? CurrentPriceEffectiveFrom { get; set; }

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

    /// <summary>Lịch sử giá bán / markdown / promotion theo thời gian hiệu lực.</summary>
    public ICollection<ProductPrice> PriceHistory { get; set; } = new List<ProductPrice>();

    /// <summary>Snapshot định giá tồn theo SKU / kho / ngày.</summary>
    public ICollection<InventoryValuationSnapshot> ValuationSnapshots { get; set; } = new List<InventoryValuationSnapshot>();
}
