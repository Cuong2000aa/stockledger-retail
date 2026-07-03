using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockLedgerRetail.Application.Caching;
using StockLedgerRetail.Caching;
using Xunit;

namespace StockLedgerRetail.Caching.Tests;

[Collection(RedisCollection.Name)]
public sealed class DistributedCacheServiceRedisTests(RedisTestFixture fixture) : RedisTestBase(fixture)
{
    [Fact]
    public async Task GetAsync_AfterSet_ReturnsCachedValue()
    {
        RequireRedis();

        await using var scope = Fixture.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var key = UniqueKey("get-set");
        var expected = new SampleDto { Id = 42, Name = "redis-hit" };

        var miss = await cache.GetAsync<SampleDto>(key);
        Assert.Null(miss);

        await cache.SetAsync(key, expected, TimeSpan.FromMinutes(5));
        var hit = await cache.GetAsync<SampleDto>(key);

        Assert.NotNull(hit);
        Assert.Equal(expected.Id, hit.Id);
        Assert.Equal(expected.Name, hit.Name);

        await cache.RemoveAsync(key);
    }

    [Fact]
    public async Task RemoveAsync_DeletesCachedValue()
    {
        RequireRedis();

        await using var scope = Fixture.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var key = UniqueKey("remove");
        await cache.SetAsync(key, new SampleDto { Id = 1, Name = "to-delete" }, TimeSpan.FromMinutes(5));

        await cache.RemoveAsync(key);
        var afterRemove = await cache.GetAsync<SampleDto>(key);

        Assert.Null(afterRemove);
    }

    [Fact]
    public async Task RemoveByPrefixAsync_RemovesTrackedKeysOnSameInstance()
    {
        RequireRedis();

        await using var scope = Fixture.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var prefix = UniqueKey("prefix-delete") + ":";
        var keyA = $"{prefix}a";
        var keyB = $"{prefix}b";
        var otherKey = UniqueKey("other");

        await cache.SetAsync(keyA, new SampleDto { Id = 1, Name = "a" }, TimeSpan.FromMinutes(5));
        await cache.SetAsync(keyB, new SampleDto { Id = 2, Name = "b" }, TimeSpan.FromMinutes(5));
        await cache.SetAsync(otherKey, new SampleDto { Id = 3, Name = "other" }, TimeSpan.FromMinutes(5));

        await cache.RemoveByPrefixAsync(prefix);

        Assert.Null(await cache.GetAsync<SampleDto>(keyA));
        Assert.Null(await cache.GetAsync<SampleDto>(keyB));
        Assert.NotNull(await cache.GetAsync<SampleDto>(otherKey));

        await cache.RemoveAsync(otherKey);
    }

    private sealed class SampleDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
