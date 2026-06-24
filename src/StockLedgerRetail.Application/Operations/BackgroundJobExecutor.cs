using System.Diagnostics;
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
    private readonly IBackgroundJobRepository _backgroundJobRepository;
    private readonly IBackgroundJobCoordinator _backgroundJobCoordinator;
    private readonly ICurrentUserContext _currentUserContext;

    public BackgroundJobExecutor(
        IBackgroundJobRepository backgroundJobRepository,
        IBackgroundJobCoordinator backgroundJobCoordinator,
        ICurrentUserContext currentUserContext)
    {
        _backgroundJobRepository = backgroundJobRepository;
        _backgroundJobCoordinator = backgroundJobCoordinator;
        _currentUserContext = currentUserContext;
    }

    public async Task ExecuteAsync(
        string jobKey,
        string triggeredBy,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        if (_backgroundJobCoordinator.IsRunning(jobKey))
        {
            return;
        }

        var setting = await _backgroundJobRepository.GetSettingByKeyAsync(jobKey, cancellationToken);
        if (setting is null)
        {
            return;
        }

        var run = new BackgroundJobRun
        {
            JobKey = jobKey,
            TriggeredBy = triggeredBy,
            Status = BackgroundJobStatuses.Running,
            StartedAtUtc = DateTime.UtcNow
        };

        _backgroundJobCoordinator.MarkRunning(jobKey);
        setting.LastStatus = BackgroundJobStatuses.Running;
        setting.LastMessage = $"Started ({triggeredBy})";
        setting.LastRunStartedAtUtc = run.StartedAtUtc;
        setting.NextRunAtUtc = null;

        await _backgroundJobRepository.InsertRunAsync(run, cancellationToken);
        await _backgroundJobRepository.UpdateSettingAsync(setting, cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await action(cancellationToken);
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

            await _backgroundJobRepository.UpdateRunAsync(run, cancellationToken);
            await _backgroundJobRepository.UpdateSettingAsync(setting, cancellationToken);
            _backgroundJobCoordinator.MarkIdle(jobKey);
            throw;
        }

        await _backgroundJobRepository.UpdateRunAsync(run, cancellationToken);
        await _backgroundJobRepository.UpdateSettingAsync(setting, cancellationToken);
        _backgroundJobCoordinator.MarkIdle(jobKey);
    }

    public string BuildManualTriggerLabel() =>
        _currentUserContext.IsAuthenticated
            ? $"{BackgroundJobTriggers.Manual}:{_currentUserContext.Email}"
            : BackgroundJobTriggers.Manual;
}
