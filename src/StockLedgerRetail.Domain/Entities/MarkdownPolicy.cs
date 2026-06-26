using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Domain.Entities;

/// <summary>
/// Chính sách giảm giá (markdown) theo brand — tiers lưu JSON trong <see cref="TiersJson"/>.
/// </summary>
public class MarkdownPolicy
{
    public Guid Id { get; set; }

    public Guid BrandId { get; set; }

    public string? RegionCode { get; set; }

    public WarehouseType? WarehouseType { get; set; }

    public int LookbackDays { get; set; } = 30;

    public int MinDaysWithoutOutbound { get; set; } = 60;

    public decimal MinOnHand { get; set; } = 1;

    public decimal? MinInventoryValueAtCost { get; set; }

    public decimal MinGrossMarginPercent { get; set; } = 10;

    public decimal MaxMarkdownPercent { get; set; } = 50;

    public bool AllowBelowCost { get; set; }

    public decimal? RequireApprovalAbovePercent { get; set; }

    /// <summary>Tỷ lệ sell-through shop so với median brand; dưới ngưỡng này dùng % giảm sâu hơn.</summary>
    public decimal SlowSellThroughThreshold { get; set; } = 0.5m;

    public string TiersJson { get; set; } = "[]";

    public bool IsActive { get; set; } = true;

    public string? Note { get; set; }

    public Brand? Brand { get; set; }
}
