namespace StockLedgerRetail.Domain.Entities;

/// <summary>Opaque refresh token (hash stored). Revocable on logout / password change.</summary>
public class AppUserRefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public AppUser User { get; set; } = null!;
}
