namespace StockLedgerRetail.Domain.Entities;

/// <summary>
/// Quy tắc luân chuyển giữa brand/kho. SourceBrandId/DestBrandId null = áp dụng mọi brand (shared DC).
/// </summary>
public class TransferPolicy
{
    public Guid Id { get; set; }

    public Guid? SourceBrandId { get; set; }

    public Guid? DestinationBrandId { get; set; }

    public bool AllowCrossBrand { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Note { get; set; }

    public Brand? SourceBrand { get; set; }

    public Brand? DestinationBrand { get; set; }
}
