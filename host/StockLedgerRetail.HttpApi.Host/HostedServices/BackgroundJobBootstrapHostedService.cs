using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Insights;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Operations;

namespace StockLedgerRetail.HttpApi.Host.HostedServices;

public class BackgroundJobBootstrapHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundJobBootstrapHostedService> _logger;

    public BackgroundJobBootstrapHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundJobBootstrapHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBackgroundJobRepository>();
        var insightOptions = scope.ServiceProvider.GetService<IOptions<InsightSnapshotOptions>>()?.Value;
        var reconciliationOptions = scope.ServiceProvider.GetService<IOptions<StockReconciliationOptions>>()?.Value;

        var defaults = new List<BackgroundJobSetting>
        {
            new()
            {
                Id = Guid.NewGuid(),
                JobKey = BackgroundJobKeys.InsightSnapshots,
                DisplayName = "Insight snapshots",
                Description = "Precomputes inventory insights for fast dashboard reads.",
                IsEnabled = insightOptions?.Enabled ?? true,
                IntervalMinutes = Math.Max(5, insightOptions?.RefreshIntervalMinutes ?? 30),
                LastStatus = BackgroundJobStatuses.Idle,
                NextRunAtUtc = DateTime.UtcNow.AddMinutes(1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                JobKey = BackgroundJobKeys.StockReconciliation,
                DisplayName = "Stock reconciliation",
                Description = "Compares ledger balances with current stock snapshots.",
                IsEnabled = reconciliationOptions?.Enabled ?? true,
                IntervalMinutes = Math.Max(5, reconciliationOptions?.IntervalMinutes ?? 60),
                LastStatus = BackgroundJobStatuses.Idle,
                NextRunAtUtc = DateTime.UtcNow.AddMinutes(2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                JobKey = BackgroundJobKeys.ReservationExpiry,
                DisplayName = "Reservation expiry",
                Description = "Releases expired POS stock reservations to free available quantity.",
                IsEnabled = true,
                IntervalMinutes = 5,
                LastStatus = BackgroundJobStatuses.Idle,
                NextRunAtUtc = DateTime.UtcNow.AddMinutes(1)
            }
        };

        await repository.EnsureDefaultSettingsAsync(defaults, cancellationToken);
        _logger.LogInformation("Background job settings initialized.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
