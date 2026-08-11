using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<AppUserRefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task InsertAsync(AppUserRefreshToken token, CancellationToken cancellationToken = default);

    Task UpdateAsync(AppUserRefreshToken token, CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(Guid userId, DateTime revokedAt, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
