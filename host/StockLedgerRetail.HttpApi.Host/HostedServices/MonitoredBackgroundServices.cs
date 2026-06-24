using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockLedgerRetail.Application.Insights;
using StockLedgerRetail.Application.Operations;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Operations;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.HttpApi.Host.HostedServices;

public class InventoryInsightsHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InventoryInsightsHostedService> _logger;

    public InventoryInsightsHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<InventoryInsightsHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        BackgroundJobLoop.RunAsync(
            BackgroundJobKeys.InsightSnapshots,
            _scopeFactory,
            _logger,
            async (services, cancellationToken) =>
            {
                var snapshotService = services.GetRequiredService<IInventoryInsightsSnapshotService>();
                await snapshotService.RefreshAllScopesAsync(cancellationToken);
            },
            stoppingToken);
}

public class StockReconciliationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StockReconciliationHostedService> _logger;

    public StockReconciliationHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<StockReconciliationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        BackgroundJobLoop.RunAsync(
            BackgroundJobKeys.StockReconciliation,
            _scopeFactory,
            _logger,
            async (services, cancellationToken) =>
            {
                var reconciliationService = services.GetRequiredService<IStockReconciliationService>();
                await reconciliationService.RunAsync(cancellationToken);
            },
            stoppingToken);
}
