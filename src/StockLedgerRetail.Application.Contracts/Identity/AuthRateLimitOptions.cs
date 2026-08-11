namespace StockLedgerRetail.Identity;

/// <summary>Rate limits for auth endpoints (brute-force protection).</summary>
public class AuthRateLimitOptions
{
    public const string SectionName = "Auth:RateLimit";

    public bool Enabled { get; set; } = true;

    /// <summary>Max login attempts per IP per window.</summary>
    public int LoginPermitLimit { get; set; } = 5;

    /// <summary>Max refresh attempts per IP per window.</summary>
    public int RefreshPermitLimit { get; set; } = 20;

    /// <summary>Fixed window length in seconds.</summary>
    public int WindowSeconds { get; set; } = 60;
}
