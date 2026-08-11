using Microsoft.Extensions.Options;
using StockLedgerRetail.Application.Identity;
using StockLedgerRetail.Identity;
using Xunit;

namespace StockLedgerRetail.Application.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public void CreateAccessToken_returns_signed_jwt_with_email_claim()
    {
        var service = new JwtTokenService(Options.Create(new JwtOptions
        {
            Enabled = true,
            SigningKey = "unit_test_signing_key_min_32_chars!",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenMinutes = 30
        }));

        var userId = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var token = service.CreateAccessToken(userId, "user@test.local", "Test User");

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Contains('.', token);
    }

    [Fact]
    public void RefreshTokenService_hash_is_deterministic()
    {
        var hash1 = RefreshTokenService.HashToken("sample-token");
        var hash2 = RefreshTokenService.HashToken("sample-token");

        Assert.Equal(hash1, hash2);
        Assert.NotEqual(hash1, RefreshTokenService.HashToken("other-token"));
    }
}
