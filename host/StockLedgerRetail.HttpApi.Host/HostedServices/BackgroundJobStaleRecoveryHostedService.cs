using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockLedgerRetail.Application.Operations;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.HttpApi.Host.HostedServices;

public class BackgroundJobStaleRecoveryHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundJobStaleRecoveryHostedService> _logger;

    public BackgroundJobStaleRecoveryHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundJobStaleRecoveryHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IBackgroundJobRepository>();
                var coordinator = scope.ServiceProvider.GetRequiredService<IBackgroundJobCoordinator>();

                var recoveredJobKeys = await repository.RecoverStaleActiveRunsAsync(stoppingToken);
                foreach (var jobKey in recoveredJobKeys)
                {
                    coordinator.MarkIdle(jobKey);
                    _logger.LogWarning("Recovered stale background job run for {JobKey}.", jobKey);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to recover stale background job runs.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
