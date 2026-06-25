namespace StockLedgerRetail.Identity;

/// <summary>Bật/tắt endpoint đăng nhập email + mật khẩu.</summary>
public class LoginOptions
{
    public const string SectionName = "Auth:Login";

    public bool Enabled { get; set; } = true;
}
