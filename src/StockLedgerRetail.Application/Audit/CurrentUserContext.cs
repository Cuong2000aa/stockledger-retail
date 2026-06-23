namespace StockLedgerRetail.Audit;

using StockLedgerRetail.Authorization;

public class CurrentUserContext : ICurrentUserContext
{
    private HashSet<string> _permissionCodes = new(StringComparer.OrdinalIgnoreCase);

    public bool IsAuthenticated => UserId.HasValue;

    public Guid? UserId { get; private set; }

    public string? Email { get; private set; }

    public string? DisplayName { get; private set; }

    public IReadOnlyCollection<string> PermissionCodes => _permissionCodes;

    public bool HasPermission(string permissionCode)
    {
        if (_permissionCodes.Contains(Authorization.PermissionCodes.SystemAdmin))
        {
            return true;
        }

        return _permissionCodes.Contains(permissionCode);
    }

    public void SetUser(Guid userId, string email, string displayName, IReadOnlyCollection<string> permissionCodes)
    {
        UserId = userId;
        Email = email;
        DisplayName = displayName;
        _permissionCodes = permissionCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
