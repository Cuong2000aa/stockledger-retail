namespace StockLedgerRetail.Services;

using StockLedgerRetail.Enums;

public interface IPermissionAuthorizationService
{
    void EnsureAuthenticated();

    void EnsurePermission(string permissionCode);

    Task EnsureCanViewInventoryDocumentsAsync(CancellationToken cancellationToken = default);

    Task EnsureCanCreateInventoryDocumentAsync(CancellationToken cancellationToken = default);

    Task EnsureCanUpdateInventoryDocumentAsync(string documentCreatedBy, CancellationToken cancellationToken = default);

    Task EnsureCanCancelInventoryDocumentAsync(
        string documentCreatedBy,
        InventoryDocumentStatus documentStatus,
        CancellationToken cancellationToken = default);

    Task EnsureCanApproveInventoryDocumentAsync(string documentCreatedBy, CancellationToken cancellationToken = default);

    Task EnsureCanReceiveTransferAsync(string documentCreatedBy, CancellationToken cancellationToken = default);

    void EnsureAdminUsersManage();

    void EnsureAdminGroupsManage();

    void EnsureAdminTeamsManage();
}
