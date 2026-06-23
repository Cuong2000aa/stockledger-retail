namespace StockLedgerRetail.Identity;

public class StubAuthOptions
{
    public const string SectionName = "Auth:StubLogin";

    public bool Enabled { get; set; } = true;

    public string Username { get; set; } = "admin";

    public string Password { get; set; } = "1234";

    /// <summary>Email của user trong DB sau khi đăng nhập stub thành công.</summary>
    public string? UserEmail { get; set; }
}
