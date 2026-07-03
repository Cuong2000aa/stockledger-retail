using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using StockLedgerRetail.Caching;
using Xunit;

namespace StockLedgerRetail.Caching.Tests;

[Collection(RedisCollection.Name)]
public sealed class RedisConnectionTests(RedisTestFixture fixture) : RedisTestBase(fixture)
{
    [Fact]
    public async Task Redis_Ping_ReturnsSuccess()
    {
        RequireRedis();

        var multiplexer = await ConnectionMultiplexer.ConnectAsync(Fixture.ConnectionString);
        var ping = await multiplexer.GetDatabase().PingAsync();

        Assert.True(ping > TimeSpan.Zero);
    }

    [Fact]
    public async Task Redis_UsesConfiguredInstanceNamePrefix()
    {
        RequireRedis();

        await using var scope = Fixture.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var key = UniqueKey("prefix");
        var payload = new CachePayload { Message = "prefix-check" };

        await cache.SetAsync(key, payload, TimeSpan.FromMinutes(1));

        var multiplexer = await ConnectionMultiplexer.ConnectAsync(Fixture.ConnectionString);
        var prefixedKey = $"{Fixture.InstanceName}{key}";
        var exists = await multiplexer.GetDatabase().KeyExistsAsync(prefixedKey);

        Assert.True(exists);

        await cache.RemoveAsync(key);
    }

    private sealed class CachePayload
    {
        public string Message { get; set; } = string.Empty;
    }
}
