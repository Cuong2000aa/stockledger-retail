using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using StockLedgerRetail.Audit;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Operations;

namespace StockLedgerRetail.Application.Operations;

public interface IBackgroundJobExecutor
{
    Task ExecuteAsync(
        string jobKey,
        string triggeredBy,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);
}

public class BackgroundJobExecutor : IBackgroundJobExecutor
{
    private static readonly TimeSpan DefaultRunTimeout = TimeSpan.FromMinutes(20);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBackgroundJobCoordinator _backgroundJobCoordinator;
    private readonly ICurrentUserContext _currentUserContext;

    public BackgroundJobExecutor(
        IServiceScopeFactory scopeFactory,
        IBackgroundJobCoordinator backgroundJobCoordinator,
        ICurrentUserContext currentUserContext)
    {
        _scopeFactory = scopeFactory;
        _backgroundJobCoordinator = backgroundJobCoordinator;
        _currentUserContext = currentUserContext;
    }

    public async Task ExecuteAsync(
        string jobKey,
        string triggeredBy,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        if (!_backgroundJobCoordinator.TryBeginRun(jobKey))
        {
            return;
        }

        BackgroundJobRun run;
        BackgroundJobSetting setting;

        using (var setupScope = _scopeFactory.CreateScope())
        {
            var repository = setupScope.ServiceProvider.GetRequiredService<IBackgroundJobRepository>();
            var loadedSetting = await repository.GetSettingByKeyAsync(jobKey, cancellationToken);
            if (loadedSetting is null)
            {
                _backgroundJobCoordinator.MarkIdle(jobKey);
                return;
            }

            setting = loadedSetting;
            run = new BackgroundJobRun
            {
                JobKey = jobKey,
                TriggeredBy = triggeredBy,
                Status = BackgroundJobStatuses.Running,
                StartedAtUtc = DateTime.UtcNow
            };

            setting.LastStatus = BackgroundJobStatuses.Running;
            setting.LastMessage = $"Started ({triggeredBy})";
            setting.LastRunStartedAtUtc = run.StartedAtUtc;
            setting.NextRunAtUtc = null;

            await repository.InsertRunAsync(run, cancellationToken);
            await repository.UpdateSettingAsync(setting, cancellationToken);
        }

        var stopwatch = Stopwatch.StartNew();
        var timeout = ResolveRunTimeout(setting);

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var workTask = action(timeoutCts.Token);
            var delayTask = Task.Delay(timeout, CancellationToken.None);
            var completedTask = await Task.WhenAny(workTask, delayTask);

            if (completedTask != workTask)
            {
                timeoutCts.Cancel();
                await PersistTimedOutAsync(run, setting, timeout, stopwatch, cancellationToken);
                return;
            }

            await workTask;
            stopwatch.Stop();

            run.Status = BackgroundJobStatuses.Succeeded;
            run.Message = "Completed successfully.";
            run.CompletedAtUtc = DateTime.UtcNow;
            run.DurationMs = stopwatch.ElapsedMilliseconds;

            setting.LastStatus = BackgroundJobStatuses.Succeeded;
            setting.LastMessage = run.Message;
            setting.LastRunCompletedAtUtc = run.CompletedAtUtc;
            setting.NextRunAtUtc = DateTime.UtcNow.AddMinutes(Math.Max(5, setting.IntervalMinutes));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await PersistTimedOutAsync(run, setting, timeout, stopwatch, cancellationToken);
            return;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            run.Status = BackgroundJobStatuses.Failed;
            run.Message = ex.Message;
            run.CompletedAtUtc = DateTime.UtcNow;
            run.DurationMs = stopwatch.ElapsedMilliseconds;

            setting.LastStatus = BackgroundJobStatuses.Failed;
            setting.LastMessage = ex.Message;
            setting.LastRunCompletedAtUtc = run.CompletedAtUtc;
            setting.NextRunAtUtc = DateTime.UtcNow.AddMinutes(Math.Max(5, setting.IntervalMinutes));

            await PersistRunOutcomeAsync(run, setting, cancellationToken);
            throw;
        }
        finally
        {
            _backgroundJobCoordinator.MarkIdle(jobKey);
        }

        await PersistRunOutcomeAsync(run, setting, cancellationToken);
    }

    public string BuildManualTriggerLabel() =>
        _currentUserContext.IsAuthenticated
            ? $"{BackgroundJobTriggers.Manual}:{_currentUserContext.Email}"
            : BackgroundJobTriggers.Manual;

    private static TimeSpan ResolveRunTimeout(BackgroundJobSetting setting) =>
        setting.JobKey switch
        {
            BackgroundJobKeys.InsightAlerts => TimeSpan.FromSeconds(30),
            BackgroundJobKeys.InsightSnapshots => TimeSpan.FromMinutes(45),
            _ => DefaultRunTimeout
        };

    private async Task PersistTimedOutAsync(
        BackgroundJobRun run,
        BackgroundJobSetting setting,
        TimeSpan timeout,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        stopwatch.Stop();
        var timeoutMessage = timeout.TotalSeconds < 90
            ? $"Timed out after {timeout.TotalSeconds:0} seconds."
            : $"Timed out after {timeout.TotalMinutes:0} minutes.";

        run.Status = BackgroundJobStatuses.Failed;
        run.Message = timeoutMessage;
        run.CompletedAtUtc = DateTime.UtcNow;
        run.DurationMs = stopwatch.ElapsedMilliseconds;

        setting.LastStatus = BackgroundJobStatuses.Failed;
        setting.LastMessage = timeoutMessage;
        setting.LastRunCompletedAtUtc = run.CompletedAtUtc;
        setting.NextRunAtUtc = DateTime.UtcNow.AddMinutes(Math.Max(5, setting.IntervalMinutes));

        await PersistRunOutcomeAsync(run, setting, cancellationToken);
    }

    private async Task PersistRunOutcomeAsync(
        BackgroundJobRun run,
        BackgroundJobSetting setting,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBackgroundJobRepository>();
        await repository.UpdateRunAsync(run, cancellationToken);
        await repository.UpdateSettingAsync(setting, cancellationToken);
    }
}
