using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StockLedgerRetail.Application.Caching;
using StockLedgerRetail.Caching;
using Xunit;

namespace StockLedgerRetail.Caching.Tests;

public sealed class CacheDisabledTests
{
    [Fact]
    public async Task WhenCacheDisabled_GetAlwaysReturnsNull()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:Enabled"] = "false",
                ["Cache:Enabled"] = "false"
            })
            .Build();

        services.AddStockLedgerRetailCaching(configuration);
        await using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<ICacheService>();
        var key = $"automation:disabled:{Guid.NewGuid():N}";

        await cache.SetAsync(key, new Payload { Value = "ignored" }, TimeSpan.FromMinutes(5));
        var value = await cache.GetAsync<Payload>(key);

        Assert.Null(value);
    }

    private sealed class Payload
    {
        public string Value { get; set; } = string.Empty;
    }
}
