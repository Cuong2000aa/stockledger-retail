using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface IAppUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<AppUser?> GetByEmailWithPermissionsAsync(string email, CancellationToken cancellationToken = default);

    Task<AppUser?> GetByIdWithAssignmentsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<AppUser>> GetListAsync(CancellationToken cancellationToken = default);

    Task InsertAsync(AppUser user, CancellationToken cancellationToken = default);

    Task UpdateAsync(AppUser user, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
