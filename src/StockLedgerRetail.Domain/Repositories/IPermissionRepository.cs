using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface IPermissionRepository
{
    Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<List<string>> GetPermissionCodesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task EnsureSeedAsync(CancellationToken cancellationToken = default);

    Task EnsureMissingPermissionsAsync(CancellationToken cancellationToken = default);
}
