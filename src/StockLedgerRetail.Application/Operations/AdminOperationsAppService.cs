using StockLedgerRetail.Application.Operations;
using StockLedgerRetail.Authorization;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Operations;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Operations;

public class AdminOperationsAppService : IAdminOperationsAppService
{
    private readonly IPermissionAuthorizationService _permissionAuthorizationService;
    private readonly IBackgroundJobRepository _backgroundJobRepository;
    private readonly IBackgroundJobCoordinator _backgroundJobCoordinator;

    public AdminOperationsAppService(
        IPermissionAuthorizationService permissionAuthorizationService,
        IBackgroundJobRepository backgroundJobRepository,
        IBackgroundJobCoordinator backgroundJobCoordinator)
    {
        _permissionAuthorizationService = permissionAuthorizationService;
        _backgroundJobRepository = backgroundJobRepository;
        _backgroundJobCoordinator = backgroundJobCoordinator;
    }

    public async Task<OperationsDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        EnsureSystemAdmin();

        var settings = await _backgroundJobRepository.GetSettingsAsync(cancellationToken);
        var recentRuns = await _backgroundJobRepository.GetRecentRunsAsync(null, 30, cancellationToken);

        return new OperationsDashboardDto
        {
            Jobs = settings.Select(MapJob).ToList(),
            RecentRuns = recentRuns.Select(MapRun).ToList()
        };
    }

    public async Task<List<BackgroundJobRunDto>> GetJobHistoryAsync(
        string jobKey,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        EnsureSystemAdmin();

        var runs = await _backgroundJobRepository.GetRecentRunsAsync(jobKey, limit, cancellationToken);
        return runs.Select(MapRun).ToList();
    }

    public async Task<BackgroundJobDto> UpdateJobAsync(
        string jobKey,
        UpdateBackgroundJobDto input,
        CancellationToken cancellationToken = default)
    {
        EnsureSystemAdmin();

        var setting = await _backgroundJobRepository.GetSettingByKeyAsync(jobKey, cancellationToken)
            ?? throw new InvalidOperationException($"Background job '{jobKey}' was not found.");

        setting.IsEnabled = input.IsEnabled;
        setting.IntervalMinutes = Math.Clamp(input.IntervalMinutes, 5, 24 * 60);

        if (!input.IsEnabled)
        {
            _backgroundJobCoordinator.ClearManualRun(jobKey);
        }

        if (setting.LastRunCompletedAtUtc.HasValue)
        {
            setting.NextRunAtUtc = setting.LastRunCompletedAtUtc.Value
                .AddMinutes(setting.IntervalMinutes);
        }
        else
        {
            setting.NextRunAtUtc = DateTime.UtcNow.AddMinutes(setting.IntervalMinutes);
        }

        await _backgroundJobRepository.UpdateSettingAsync(setting, cancellationToken);
        return MapJob(setting);
    }

    public Task<TriggerBackgroundJobResponseDto> TriggerJobAsync(
        string jobKey,
        CancellationToken cancellationToken = default)
    {
        EnsureSystemAdmin();

        if (_backgroundJobCoordinator.IsRunning(jobKey))
        {
            return Task.FromResult(new TriggerBackgroundJobResponseDto
            {
                JobKey = jobKey,
                Accepted = false,
                Message = "Job is already running."
            });
        }

        if (_backgroundJobCoordinator.IsManualRunPending(jobKey))
        {
            return Task.FromResult(new TriggerBackgroundJobResponseDto
            {
                JobKey = jobKey,
                Accepted = false,
                Message = "A manual run is already queued."
            });
        }

        _backgroundJobCoordinator.RequestManualRun(jobKey);

        return Task.FromResult(new TriggerBackgroundJobResponseDto
        {
            JobKey = jobKey,
            Accepted = true,
            Message = "Manual run requested. The worker will pick it up shortly."
        });
    }

    private void EnsureSystemAdmin() =>
        _permissionAuthorizationService.EnsurePermission(PermissionCodes.SystemAdmin);

    private BackgroundJobDto MapJob(Domain.Entities.BackgroundJobSetting setting) =>
        new()
        {
            JobKey = setting.JobKey,
            DisplayName = setting.DisplayName,
            Description = setting.Description,
            IsEnabled = setting.IsEnabled,
            IntervalMinutes = setting.IntervalMinutes,
            LastStatus = setting.LastStatus,
            LastMessage = setting.LastMessage,
            LastRunStartedAtUtc = setting.LastRunStartedAtUtc,
            LastRunCompletedAtUtc = setting.LastRunCompletedAtUtc,
            NextRunAtUtc = setting.NextRunAtUtc,
            IsRunning = _backgroundJobCoordinator.IsRunning(setting.JobKey),
            ManualRunRequested = _backgroundJobCoordinator.IsManualRunPending(setting.JobKey)
        };

    private static BackgroundJobRunDto MapRun(Domain.Entities.BackgroundJobRun run) =>
        new()
        {
            Id = run.Id,
            JobKey = run.JobKey,
            TriggeredBy = run.TriggeredBy,
            Status = run.Status,
            Message = run.Message,
            StartedAtUtc = run.StartedAtUtc,
            CompletedAtUtc = run.CompletedAtUtc,
            DurationMs = run.DurationMs
        };
}
