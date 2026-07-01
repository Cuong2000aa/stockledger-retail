namespace StockLedgerRetail.Services;

public sealed record WarehouseListScope(Guid? WarehouseId, IReadOnlyCollection<Guid>? ScopedWarehouseIds);

public interface IWarehouseScopeService
{
    bool HasUnrestrictedWarehouseAccess();

    Guid? NormalizeWarehouseFilter(Guid? requestedWarehouseId);

    WarehouseListScope ResolveListScope(Guid? requestedWarehouseId);

    Guid? GetDefaultWarehouseId();

    void EnsureWarehouseAccess(Guid warehouseId);

    IReadOnlyCollection<Guid>? GetWarehouseFilterForLists();
}
