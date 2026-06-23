using StockLedgerRetail.Audit;
using StockLedgerRetail.Authorization;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Services;

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
            throw new UnauthorizedAccessException($"Missing permission '{permissionCode}'.");
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
        await EnsureOwnOrTeamManagedAsync(documentCreatedBy, PermissionCodes.InventoryDocumentsUpdate, cancellationToken);
    }

    public async Task EnsureCanCancelInventoryDocumentAsync(
        string documentCreatedBy,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        await EnsureOwnOrTeamManagedAsync(documentCreatedBy, PermissionCodes.InventoryDocumentsCancel, cancellationToken);
    }

    public async Task EnsureCanApproveInventoryDocumentAsync(
        string documentCreatedBy,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        if (_currentUser.HasPermission(PermissionCodes.InventoryDocumentsApprove))
        {
            return;
        }

        if (_currentUser.HasPermission(PermissionCodes.InventoryDocumentsApproveTeam)
            && _currentUser.UserId.HasValue
            && !string.Equals(documentCreatedBy, _currentUser.Email, StringComparison.OrdinalIgnoreCase)
            && await _teamRepository.IsLeaderOfMemberAsync(
                _currentUser.UserId.Value,
                documentCreatedBy,
                cancellationToken))
        {
            return;
        }

        throw new UnauthorizedAccessException("You are not allowed to approve this document.");
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

        throw new UnauthorizedAccessException("You are not allowed to receive this transfer.");
    }

    public void EnsureAdminUsersManage() => EnsurePermission(PermissionCodes.AdminUsersManage);

    public void EnsureAdminGroupsManage() => EnsurePermission(PermissionCodes.AdminGroupsManage);

    public void EnsureAdminTeamsManage() => EnsurePermission(PermissionCodes.AdminTeamsManage);

    private async Task EnsureOwnOrTeamManagedAsync(
        string documentCreatedBy,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(permissionCode))
        {
            throw new UnauthorizedAccessException($"Missing permission '{permissionCode}'.");
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

        throw new UnauthorizedAccessException(
            "You can only manage your own documents or team members' documents as team leader.");
    }
}
