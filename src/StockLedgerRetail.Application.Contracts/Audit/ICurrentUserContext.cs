namespace StockLedgerRetail.Audit;

/// <summary>
/// Người dùng hiện tại (theo email) và quyền đã load từ DB.
/// </summary>
public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? Email { get; }

    string? DisplayName { get; }

    IReadOnlyCollection<string> PermissionCodes { get; }

    bool HasPermission(string permissionCode);

    void SetUser(Guid userId, string email, string displayName, IReadOnlyCollection<string> permissionCodes);
}
