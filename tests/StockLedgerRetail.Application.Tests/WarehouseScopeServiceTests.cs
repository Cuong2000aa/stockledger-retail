using StockLedgerRetail.Application.Authorization;
using StockLedgerRetail.Audit;
using StockLedgerRetail.Authorization;
using Xunit;

namespace StockLedgerRetail.Application.Tests;

public class WarehouseScopeServiceTests
{
    private static readonly Guid WarehouseA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid WarehouseB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Unauthenticated_user_has_unrestricted_access()
    {
        var service = CreateService(isAuthenticated: false, permissions: [], allowed: []);

        service.EnsureWarehouseAccess(WarehouseA);

        var scope = service.ResolveListScope(null);
        Assert.Null(scope.WarehouseId);
        Assert.Null(scope.ScopedWarehouseIds);
    }

    [Fact]
    public void System_admin_has_unrestricted_access()
    {
        var service = CreateService(
            isAuthenticated: true,
            permissions: [PermissionCodes.SystemAdmin],
            allowed: [WarehouseA]);

        service.EnsureWarehouseAccess(WarehouseB);

        var scope = service.ResolveListScope(null);
        Assert.Null(scope.WarehouseId);
        Assert.Null(scope.ScopedWarehouseIds);
    }

    [Fact]
    public void Scoped_user_cannot_access_other_warehouse()
    {
        var service = CreateService(
            isAuthenticated: true,
            permissions: [],
            allowed: [WarehouseA]);

        Assert.Throws<UnauthorizedAccessException>(() => service.EnsureWarehouseAccess(WarehouseB));
    }

    [Fact]
    public void ResolveListScope_returns_scoped_ids_for_multi_warehouse_user()
    {
        var service = CreateService(
            isAuthenticated: true,
            permissions: [],
            allowed: [WarehouseA, WarehouseB]);

        var scope = service.ResolveListScope(null);

        Assert.Null(scope.WarehouseId);
        Assert.NotNull(scope.ScopedWarehouseIds);
        Assert.Equal(2, scope.ScopedWarehouseIds!.Count);
    }

    [Fact]
    public void ResolveListScope_pins_single_warehouse_when_user_has_one()
    {
        var service = CreateService(
            isAuthenticated: true,
            permissions: [],
            allowed: [WarehouseA]);

        var scope = service.ResolveListScope(null);

        Assert.Equal(WarehouseA, scope.WarehouseId);
        Assert.Null(scope.ScopedWarehouseIds);
    }

    [Fact]
    public void GetDefaultWarehouseId_prefers_primary_when_allowed()
    {
        var service = CreateService(
            isAuthenticated: true,
            permissions: [],
            allowed: [WarehouseA, WarehouseB],
            primaryWarehouseId: WarehouseB);

        Assert.Equal(WarehouseB, service.GetDefaultWarehouseId());
    }

    [Fact]
    public void NormalizeWarehouseFilter_rejects_out_of_scope_request()
    {
        var service = CreateService(
            isAuthenticated: true,
            permissions: [],
            allowed: [WarehouseA]);

        Assert.Throws<UnauthorizedAccessException>(() => service.NormalizeWarehouseFilter(WarehouseB));
    }

    private static WarehouseScopeService CreateService(
        bool isAuthenticated,
        IReadOnlyCollection<string> permissions,
        IReadOnlyCollection<Guid> allowed,
        Guid? primaryWarehouseId = null) =>
        new(new TestCurrentUser(isAuthenticated, permissions), new TestWarehouseScope(allowed, primaryWarehouseId));

    private sealed class TestCurrentUser(bool isAuthenticated, IReadOnlyCollection<string> permissionCodes)
        : ICurrentUserContext
    {
        public bool IsAuthenticated => isAuthenticated;
        public Guid? UserId => isAuthenticated ? Guid.NewGuid() : null;
        public string? Email => isAuthenticated ? "test@stockledger.local" : null;
        public string? DisplayName => isAuthenticated ? "Test User" : null;
        public IReadOnlyCollection<string> PermissionCodes => permissionCodes;
        public bool HasPermission(string permissionCode) => permissionCodes.Contains(permissionCode);
        public void SetUser(Guid userId, string email, string displayName, IReadOnlyCollection<string> permissionCodes) { }
    }

    private sealed class TestWarehouseScope(IReadOnlyCollection<Guid> allowed, Guid? primaryWarehouseId)
        : IUserWarehouseScopeContext
    {
        public IReadOnlyCollection<Guid>? AllowedWarehouseIds => allowed;
        public Guid? PrimaryWarehouseId => primaryWarehouseId;
    }
}
