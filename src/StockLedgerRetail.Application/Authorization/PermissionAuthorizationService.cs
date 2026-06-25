using StockLedgerRetail.Audit;
using StockLedgerRetail.Authorization;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Services;
using static StockLedgerRetail.BusinessErrorCodes;

namespace StockLedgerRetail.Application.Authorization;

public class PermissionAuthorizationService : IPermissionAuthorizationService
{
    private readonly ICurrentUserContext _currentUser;
    private readonly ITeamRepository _teamRepository;

    public PermissionAuthorizationService(
        ICurrentUserContext currentUser,
        ITeamRepository teamRepository)
    {
        _currentUser = currentUser;
        _teamRepository = teamRepository;
    }

    public void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User email is required. Send header X-User-Email.");
        }
    }

    public void EnsurePermission(string permissionCode)
    {
        EnsureAuthenticated();

        if (!_currentUser.HasPermission(permissionCode))
        {
            throw new UnauthorizedAccessException(MissingPermission(permissionCode));
        }
    }

    public Task EnsureCanViewInventoryDocumentsAsync(CancellationToken cancellationToken = default)
    {
        EnsurePermission(PermissionCodes.InventoryDocumentsView);
        return Task.CompletedTask;
    }

    public Task EnsureCanCreateInventoryDocumentAsync(CancellationToken cancellationToken = default)
    {
        EnsurePermission(PermissionCodes.InventoryDocumentsCreate);
        return Task.CompletedTask;
    }

    public async Task EnsureCanUpdateInventoryDocumentAsync(
        string documentCreatedBy,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        if (await CanBypassDocumentOwnershipAsync(documentCreatedBy, cancellationToken))
        {
            return;
        }

        await EnsureOwnOrTeamManagedAsync(documentCreatedBy, PermissionCodes.InventoryDocumentsUpdate, cancellationToken);
    }

    public async Task EnsureCanCancelInventoryDocumentAsync(
        string documentCreatedBy,
        InventoryDocumentStatus documentStatus,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        // Draft/Pending: người duyệt hoặc trưởng nhóm có thể hủy dù không phải người tạo.
        if (documentStatus is InventoryDocumentStatus.Draft or InventoryDocumentStatus.Pending
            && await CanBypassDocumentOwnershipAsync(documentCreatedBy, cancellationToken))
        {
            return;
        }

        await EnsureOwnOrTeamManagedAsync(documentCreatedBy, PermissionCodes.InventoryDocumentsCancel, cancellationToken);
    }

    public async Task EnsureCanApproveInventoryDocumentAsync(
        string documentCreatedBy,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        if (await CanBypassDocumentOwnershipAsync(documentCreatedBy, cancellationToken))
        {
            return;
        }

        throw new UnauthorizedAccessException(CannotApproveDocument);
    }

    public async Task EnsureCanReceiveTransferAsync(
        string documentCreatedBy,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        if (_currentUser.HasPermission(PermissionCodes.InventoryDocumentsReceiveTransfer))
        {
            if (_currentUser.HasPermission(PermissionCodes.InventoryDocumentsApprove))
            {
                return;
            }

            if (_currentUser.UserId.HasValue
                && (string.Equals(documentCreatedBy, _currentUser.Email, StringComparison.OrdinalIgnoreCase)
                    || await _teamRepository.IsLeaderOfMemberAsync(
                        _currentUser.UserId.Value,
                        documentCreatedBy,
                        cancellationToken)))
            {
                return;
            }
        }

        throw new UnauthorizedAccessException(CannotReceiveTransfer);
    }

    public void EnsureAdminUsersManage() => EnsurePermission(PermissionCodes.AdminUsersManage);

    public void EnsureAdminGroupsManage() => EnsurePermission(PermissionCodes.AdminGroupsManage);

    public void EnsureAdminTeamsManage() => EnsurePermission(PermissionCodes.AdminTeamsManage);

    /// <summary>
    /// Cho phép duyệt/hủy phiếu Draft hoặc Pending của người khác khi có quyền duyệt toàn hệ thống hoặc trưởng nhóm.
    /// </summary>
    private async Task<bool> CanBypassDocumentOwnershipAsync(
        string documentCreatedBy,
        CancellationToken cancellationToken)
    {
        if (_currentUser.HasPermission(PermissionCodes.InventoryDocumentsApprove))
        {
            return true;
        }

        if (_currentUser.HasPermission(PermissionCodes.InventoryDocumentsApproveTeam)
            && _currentUser.UserId.HasValue
            && !string.Equals(documentCreatedBy, _currentUser.Email, StringComparison.OrdinalIgnoreCase)
            && await _teamRepository.IsLeaderOfMemberAsync(
                _currentUser.UserId.Value,
                documentCreatedBy,
                cancellationToken))
        {
            return true;
        }

        return false;
    }

    private async Task EnsureOwnOrTeamManagedAsync(
        string documentCreatedBy,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(permissionCode))
        {
            throw new UnauthorizedAccessException(MissingPermission(permissionCode));
        }

        if (string.Equals(documentCreatedBy, _currentUser.Email, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (_currentUser.HasPermission(PermissionCodes.InventoryDocumentsApproveTeam)
            && _currentUser.UserId.HasValue
            && await _teamRepository.IsLeaderOfMemberAsync(
                _currentUser.UserId.Value,
                documentCreatedBy,
                cancellationToken))
        {
            return;
        }

        throw new UnauthorizedAccessException(CannotManageDocument);
    }
}
