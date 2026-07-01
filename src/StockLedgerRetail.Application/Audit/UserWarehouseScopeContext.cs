namespace StockLedgerRetail.Audit;

public class UserWarehouseScopeContext : IUserWarehouseScopeContext
{
    public IReadOnlyCollection<Guid>? AllowedWarehouseIds { get; private set; }

    public Guid? PrimaryWarehouseId { get; private set; }

    public void SetUnrestricted()
    {
        AllowedWarehouseIds = null;
        PrimaryWarehouseId = null;
    }

    public void SetAssignments(IReadOnlyCollection<Guid> warehouseIds, Guid? primaryWarehouseId)
    {
        AllowedWarehouseIds = warehouseIds.Count == 0 ? Array.Empty<Guid>() : warehouseIds.ToList();
        PrimaryWarehouseId = primaryWarehouseId;
    }
}
