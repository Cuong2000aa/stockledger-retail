using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Entities;

/// <summary>
/// Lịch sử giá vốn theo SKU — ghi nhận thay đổi cost theo nguồn và khoảng thời gian hiệu lực.
/// Chuẩn bị cho đồng bộ ERP/POS/Purchase System; chưa có nghiệp vụ ghi sổ tại phase này.
/// </summary>
public class ProductCostHistory
{
    public Guid Id { get; set; }

    public Guid ProductVariantId { get; set; }

    public decimal CostPrice { get; set; }

    public CostSource CostSource { get; set; }

    public DateTime EffectiveFrom { get; set; }

    /// <summary>Null nghĩa là bản ghi đang hiệu lực (chưa có ngày kết thúc).</summary>
    public DateTime? EffectiveTo { get; set; }

    public ProductVariant? ProductVariant { get; set; }
}
