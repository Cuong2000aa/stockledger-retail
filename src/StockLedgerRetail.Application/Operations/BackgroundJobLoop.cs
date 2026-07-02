using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Operations;

namespace StockLedgerRetail.Application.Operations;

public static class BackgroundJobLoop
{
    public static async Task RunAsync(
        string jobKey,
        IServiceScopeFactory scopeFactory,
        ILogger logger,
        Func<IServiceProvider, CancellationToken, Task> work,
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IBackgroundJobRepository>();
                var coordinator = scope.ServiceProvider.GetRequiredService<IBackgroundJobCoordinator>();
                var executor = scope.ServiceProvider.GetRequiredService<IBackgroundJobExecutor>();

                var setting = await repository.GetSettingByKeyAsync(jobKey, stoppingToken);
                if (setting is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    continue;
                }

                if (coordinator.IsRunning(jobKey))
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                var trigger = BackgroundJobTriggers.Scheduled;
                var shouldRun = false;

                if (!setting.IsEnabled)
                {
                    if (coordinator.TryConsumeManualRun(jobKey))
                    {
                        trigger = BackgroundJobTriggers.Manual;
                        shouldRun = true;
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                        continue;
                    }
                }
                else
                {
                    var deadline = setting.NextRunAtUtc ?? DateTime.UtcNow;

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        if (coordinator.TryConsumeManualRun(jobKey))
                        {
                            trigger = BackgroundJobTriggers.Manual;
                            shouldRun = true;
                            break;
                        }

                        if (DateTime.UtcNow >= deadline)
                        {
                            shouldRun = true;
                            break;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                }

                if (!shouldRun || stoppingToken.IsCancellationRequested)
                {
                    continue;
                }

                await executor.ExecuteAsync(
                    jobKey,
                    trigger,
                    async ct =>
                    {
                        using var workScope = scopeFactory.CreateScope();
                        await work(workScope.ServiceProvider, ct);
                    },
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background job loop failed for {JobKey}.", jobKey);
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }
}
