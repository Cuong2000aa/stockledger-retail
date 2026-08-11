namespace StockLedgerRetail.Identity;

/// <summary>JWT access token + refresh token settings.</summary>
public class JwtOptions
{
    public const string SectionName = "Auth:Jwt";

    public bool Enabled { get; set; } = true;

    public string Issuer { get; set; } = "StockLedgerRetail";

    public string Audience { get; set; } = "StockLedgerRetail.Web";

    /// <summary>HS256 signing key — min 32 chars; set via env in production.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 60;

    public int RefreshTokenDays { get; set; } = 7;
}

/// <summary>Auth behavior flags (legacy header, require login).</summary>
public class AuthOptions
{
    public const string SectionName = "Auth";

    public bool RequireUserEmail { get; set; } = true;

    /// <summary>Allow X-User-Email header without JWT — dev/integration only.</summary>
    public bool AllowLegacyEmailHeader { get; set; }
}
