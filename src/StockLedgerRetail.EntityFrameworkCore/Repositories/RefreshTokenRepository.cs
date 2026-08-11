using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public RefreshTokenRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AppUserRefreshToken?> GetActiveByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        _dbContext.AppUserRefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.TokenHash == tokenHash
                     && x.RevokedAt == null
                     && x.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

    public async Task InsertAsync(AppUserRefreshToken token, CancellationToken cancellationToken = default) =>
        await _dbContext.AppUserRefreshTokens.AddAsync(token, cancellationToken);

    public Task UpdateAsync(AppUserRefreshToken token, CancellationToken cancellationToken = default)
    {
        _dbContext.AppUserRefreshTokens.Update(token);
        return Task.CompletedTask;
    }

    public async Task RevokeAllForUserAsync(
        Guid userId,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.AppUserRefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.RevokedAt, revokedAt),
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
