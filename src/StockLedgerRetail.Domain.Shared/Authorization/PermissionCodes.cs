namespace StockLedgerRetail.Authorization;

/// <summary>
/// Mã quyền lưu trong DB — dùng cho kiểm tra authorization.
/// </summary>
public static class PermissionCodes
{
    public const string SystemAdmin = "system.admin";

    public const string InventoryDocumentsView = "inventory.documents.view";
    public const string InventoryDocumentsCreate = "inventory.documents.create";
    public const string InventoryDocumentsUpdate = "inventory.documents.update";
    public const string InventoryDocumentsCancel = "inventory.documents.cancel";
    public const string InventoryDocumentsApprove = "inventory.documents.approve";
    public const string InventoryDocumentsApproveTeam = "inventory.documents.approve.team";
    public const string InventoryDocumentsReceiveTransfer = "inventory.documents.receive-transfer";

    public const string InventoryScopeAllWarehouses = "inventory.scope.all_warehouses";

    public const string AdminUsersManage = "admin.users.manage";
    public const string AdminGroupsManage = "admin.groups.manage";
    public const string AdminTeamsManage = "admin.teams.manage";

    public static IReadOnlyList<string> All { get; } =
    [
        SystemAdmin,
        InventoryDocumentsView,
        InventoryDocumentsCreate,
        InventoryDocumentsUpdate,
        InventoryDocumentsCancel,
        InventoryDocumentsApprove,
        InventoryDocumentsApproveTeam,
        InventoryDocumentsReceiveTransfer,
        InventoryScopeAllWarehouses,
        AdminUsersManage,
        AdminGroupsManage,
        AdminTeamsManage
    ];
}
