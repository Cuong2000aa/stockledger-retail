using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface IPermissionGroupRepository
{
    Task<PermissionGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PermissionGroup?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<List<PermissionGroup>> GetListAsync(CancellationToken cancellationToken = default);

    Task AssignUserToGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default);

    Task RemoveUserFromGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
