namespace StockLedgerRetail.Audit;

public class BrandScopeContext : IBrandScopeContext
{
    public Guid? BrandId { get; set; }

    public IReadOnlyCollection<Guid>? WarehouseIds { get; set; }

    public string? RegionCode { get; set; }
}
