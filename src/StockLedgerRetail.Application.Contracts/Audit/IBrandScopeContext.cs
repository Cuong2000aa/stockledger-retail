namespace StockLedgerRetail.Audit;

/// <summary>
/// Phạm vi brand/kho từ header (Phase 4 RBAC-lite). Null = không giới hạn.
/// </summary>
public interface IBrandScopeContext
{
    Guid? BrandId { get; }

    IReadOnlyCollection<Guid>? WarehouseIds { get; }

    string? RegionCode { get; }
}
