using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Identity;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Identity;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly JwtOptions _jwtOptions;

    public RefreshTokenService(
        IRefreshTokenRepository refreshTokenRepository,
        IOptions<JwtOptions> jwtOptions)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<string> IssueAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var plainToken = GeneratePlainToken();
        var now = DateTime.UtcNow;
        var entity = new AppUserRefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(plainToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_jwtOptions.RefreshTokenDays)
        };

        await _refreshTokenRepository.InsertAsync(entity, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
        return plainToken;
    }

    public async Task<Guid?> ValidateAndRevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var tokenHash = HashToken(refreshToken);
        var stored = await _refreshTokenRepository.GetActiveByHashAsync(tokenHash, cancellationToken);
        if (stored is null || !stored.User.IsActive)
        {
            return null;
        }

        stored.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.UpdateAsync(stored, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
        return stored.UserId;
    }

    public Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _refreshTokenRepository.RevokeAllForUserAsync(userId, DateTime.UtcNow, cancellationToken);

    public static string HashToken(string plainToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainToken));
        return Convert.ToHexString(bytes);
    }

    private static string GeneratePlainToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
