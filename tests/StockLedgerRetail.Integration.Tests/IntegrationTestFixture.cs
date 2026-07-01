using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StockLedgerRetail.Application.Seed;
using StockLedgerRetail.Audit;
using StockLedgerRetail.Authorization;
using StockLedgerRetail.EntityFrameworkCore;
using StockLedgerRetail.Services;
using Xunit;

namespace StockLedgerRetail.Integration.Tests;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    public static readonly Guid TestUserId = Guid.Parse("f0000001-0001-4001-8001-000000000001");
    public const string TestUserEmail = "integration-test@stockledger.local";

    private static readonly IReadOnlyCollection<string> TestPermissions =
    [
        PermissionCodes.SystemAdmin,
        PermissionCodes.InventoryScopeAllWarehouses,
        PermissionCodes.InventoryDocumentsView,
        PermissionCodes.InventoryDocumentsCreate,
        PermissionCodes.InventoryDocumentsApprove
    ];

    public bool DatabaseReady { get; private set; }

    public string? DatabaseSkipReason { get; private set; }

    public IServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var configuration = BuildConfiguration();
        var connectionString = ResolveConnectionString(configuration);

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddStockLedgerRetailEntityFrameworkCore(connectionString);
        services.AddStockLedgerRetailApplication(configuration);

        Services = services.BuildServiceProvider();

        try
        {
            await using var scope = Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<StockLedgerRetailDbContext>();
            if (!await db.Database.CanConnectAsync())
            {
                DatabaseSkipReason = "Cannot connect to Postgres. Set STOCKLEDGER_TEST_CONNECTION or update appsettings.Testing.json.";
                return;
            }

            await db.Database.MigrateAsync();
            ConfigureAuth(scope.ServiceProvider);
            await scope.ServiceProvider.GetRequiredService<IFbDataSeedService>().EnsureSeedAsync();
            DatabaseReady = true;
        }
        catch (Exception ex)
        {
            DatabaseSkipReason = $"Integration DB setup failed: {ex.Message}";
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public AsyncServiceScope CreateScope()
    {
        var scope = Services.CreateAsyncScope();
        ConfigureAuth(scope.ServiceProvider);
        return scope;
    }

    private static void ConfigureAuth(IServiceProvider provider)
    {
        if (provider.GetRequiredService<ICurrentUserContext>() is not CurrentUserContext currentUser
            || provider.GetRequiredService<IUserWarehouseScopeContext>() is not UserWarehouseScopeContext warehouseScope)
        {
            throw new InvalidOperationException("Integration tests require concrete audit scope types.");
        }

        currentUser.SetUser(
            TestUserId,
            TestUserEmail,
            "Integration Test",
            TestPermissions);

        warehouseScope.SetUnrestricted();
    }

    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Testing.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var fromEnv = Environment.GetEnvironmentVariable("STOCKLEDGER_TEST_CONNECTION");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        return configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Integration tests require ConnectionStrings:Default or STOCKLEDGER_TEST_CONNECTION.");
    }
}

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "Integration";
}
