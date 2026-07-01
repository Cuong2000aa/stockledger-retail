using StockLedgerRetail.Audit;
using StockLedgerRetail.Authorization;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Authorization;

public class WarehouseScopeService : IWarehouseScopeService
{
    private readonly ICurrentUserContext _currentUser;
    private readonly IUserWarehouseScopeContext _warehouseScope;

    public WarehouseScopeService(
        ICurrentUserContext currentUser,
        IUserWarehouseScopeContext warehouseScope)
    {
        _currentUser = currentUser;
        _warehouseScope = warehouseScope;
    }

    public bool HasUnrestrictedWarehouseAccess() =>
        !_currentUser.IsAuthenticated
        || _currentUser.HasPermission(PermissionCodes.SystemAdmin)
        || _currentUser.HasPermission(PermissionCodes.InventoryScopeAllWarehouses)
        || _warehouseScope.AllowedWarehouseIds is null;

    public Guid? NormalizeWarehouseFilter(Guid? requestedWarehouseId)
    {
        if (requestedWarehouseId.HasValue)
        {
            EnsureWarehouseAccess(requestedWarehouseId.Value);
            return requestedWarehouseId;
        }

        if (HasUnrestrictedWarehouseAccess())
        {
            return null;
        }

        var allowed = GetAllowedWarehouseIds();
        if (allowed.Count == 0)
        {
            return null;
        }

        return allowed.Count == 1 ? allowed.First() : null;
    }

    public WarehouseListScope ResolveListScope(Guid? requestedWarehouseId)
    {
        var normalized = NormalizeWarehouseFilter(requestedWarehouseId);
        if (normalized.HasValue)
        {
            return new WarehouseListScope(normalized, null);
        }

        var scoped = GetWarehouseFilterForLists();
        if (scoped is { Count: 1 })
        {
            return new WarehouseListScope(scoped.First(), null);
        }

        return new WarehouseListScope(null, scoped);
    }

    public Guid? GetDefaultWarehouseId()
    {
        if (HasUnrestrictedWarehouseAccess())
        {
            return null;
        }

        var allowed = GetAllowedWarehouseIds();
        if (allowed.Count == 0)
        {
            return null;
        }

        if (_warehouseScope.PrimaryWarehouseId.HasValue
            && allowed.Contains(_warehouseScope.PrimaryWarehouseId.Value))
        {
            return _warehouseScope.PrimaryWarehouseId;
        }

        return allowed.Count == 1 ? allowed.First() : null;
    }

    public void EnsureWarehouseAccess(Guid warehouseId)
    {
        if (HasUnrestrictedWarehouseAccess())
        {
            return;
        }

        if (!GetAllowedWarehouseIds().Contains(warehouseId))
        {
            throw new UnauthorizedAccessException("You do not have access to this warehouse.");
        }
    }

    public IReadOnlyCollection<Guid>? GetWarehouseFilterForLists()
    {
        if (HasUnrestrictedWarehouseAccess())
        {
            return null;
        }

        var allowed = GetAllowedWarehouseIds();
        return allowed.Count == 0 ? Array.Empty<Guid>() : allowed;
    }

    private IReadOnlyCollection<Guid> GetAllowedWarehouseIds() =>
        _warehouseScope.AllowedWarehouseIds ?? Array.Empty<Guid>();
}
