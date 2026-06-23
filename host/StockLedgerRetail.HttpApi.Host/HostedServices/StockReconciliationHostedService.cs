using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.HttpApi.Host.HostedServices;

/// <summary>Chạy đối soát tồn định kỳ — ghi log khi phát hiện lệch.</summary>
public class StockReconciliationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<StockReconciliationOptions> _options;
    private readonly ILogger<StockReconciliationHostedService> _logger;

    public StockReconciliationHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<StockReconciliationOptions> options,
        ILogger<StockReconciliationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("Stock reconciliation background job is disabled.");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(5, _options.Value.IntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stock reconciliation background job failed.");
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IStockReconciliationService>();
        await service.RunAsync(cancellationToken);
    }
}
