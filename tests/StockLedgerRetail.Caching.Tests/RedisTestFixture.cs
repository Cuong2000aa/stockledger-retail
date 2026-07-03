using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using StockLedgerRetail.Application.Caching;
using StockLedgerRetail.Caching;
using StockLedgerRetail.Caching.Tests.Support;
using StockLedgerRetail.Domain.Repositories;
using Xunit;

namespace StockLedgerRetail.Caching.Tests;

public sealed class RedisTestFixture : IAsyncLifetime
{
    public bool RedisReady { get; private set; }

    public string? RedisSkipReason { get; private set; }

    public string ConnectionString { get; private set; } = string.Empty;

    public string InstanceName { get; private set; } = string.Empty;

    public IServiceProvider Services { get; private set; } = null!;

    public FakeAppUserRepository FakeUserRepository { get; private set; } = new();

    public async Task InitializeAsync()
    {
        var configuration = BuildConfiguration();
        ConnectionString = ResolveRedisConnection(configuration);
        InstanceName = configuration.GetSection("Redis")["InstanceName"] ?? "test:";

        try
        {
            var multiplexer = await ConnectionMultiplexer.ConnectAsync(ConnectionString);
            var ping = await multiplexer.GetDatabase().PingAsync();
            if (ping == TimeSpan.Zero)
            {
                RedisSkipReason = $"Redis ping failed for {ConnectionString}.";
                return;
            }
        }
        catch (Exception ex)
        {
            RedisSkipReason =
                $"Redis unavailable at {ConnectionString}. Run scripts/dev-up.ps1 or docker compose -f docker-compose.dev.yml up -d redis. ({ex.Message})";
            return;
        }

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<IAppUserRepository>(FakeUserRepository);
        services.AddStockLedgerRetailCaching(configuration);

        Services = services.BuildServiceProvider();
        RedisReady = true;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public AsyncServiceScope CreateScope() => Services.CreateAsyncScope();

    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Testing.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

    private static string ResolveRedisConnection(IConfiguration configuration)
    {
        var fromEnv = Environment.GetEnvironmentVariable("STOCKLEDGER_REDIS_CONNECTION");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        return configuration.GetSection("Redis")["ConnectionString"]
            ?? "127.0.0.1:6379";
    }
}

[CollectionDefinition(Name)]
public sealed class RedisCollection : ICollectionFixture<RedisTestFixture>
{
    public const string Name = "Redis";
}

public abstract class RedisTestBase(RedisTestFixture fixture)
{
    protected RedisTestFixture Fixture { get; } = fixture;

    protected void RequireRedis()
    {
        Assert.True(
            Fixture.RedisReady,
            Fixture.RedisSkipReason ?? "Redis is not available.");
    }

    protected static string UniqueKey(string suffix) => $"automation:{suffix}:{Guid.NewGuid():N}";
}
