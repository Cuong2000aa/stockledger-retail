namespace StockLedgerRetail.Services;

public interface IJwtTokenService
{
    string CreateAccessToken(Guid userId, string email, string displayName);

    DateTime GetAccessTokenExpiryUtc();
}
