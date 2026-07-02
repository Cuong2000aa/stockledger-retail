using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Operations;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class BackgroundJobRepository : IBackgroundJobRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public BackgroundJobRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<BackgroundJobSetting>> GetSettingsAsync(CancellationToken cancellationToken = default) =>
        _dbContext.BackgroundJobSettings
            .AsNoTracking()
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

    public Task<BackgroundJobSetting?> GetSettingByKeyAsync(
        string jobKey,
        CancellationToken cancellationToken = default) =>
        _dbContext.BackgroundJobSettings
            .FirstOrDefaultAsync(x => x.JobKey == jobKey, cancellationToken);

    public async Task UpdateSettingAsync(BackgroundJobSetting setting, CancellationToken cancellationToken = default)
    {
        setting.UpdatedAtUtc = DateTime.UtcNow;
        _dbContext.BackgroundJobSettings.Update(setting);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task InsertRunAsync(BackgroundJobRun run, CancellationToken cancellationToken = default)
    {
        if (run.Id == Guid.Empty)
        {
            run.Id = Guid.NewGuid();
        }

        await _dbContext.BackgroundJobRuns.AddAsync(run, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRunAsync(BackgroundJobRun run, CancellationToken cancellationToken = default)
    {
        _dbContext.BackgroundJobRuns.Update(run);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<BackgroundJobRun>> GetRecentRunsAsync(
        string? jobKey,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.BackgroundJobRuns.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(jobKey))
        {
            query = query.Where(x => x.JobKey == jobKey);
        }

        return query
            .OrderByDescending(x => x.StartedAtUtc)
            .Take(Math.Clamp(limit, 1, 200))
            .ToListAsync(cancellationToken);
    }

    public async Task EnsureDefaultSettingsAsync(
        IReadOnlyList<BackgroundJobSetting> defaults,
        CancellationToken cancellationToken = default)
    {
        var existingKeys = await _dbContext.BackgroundJobSettings
            .Select(x => x.JobKey)
            .ToListAsync(cancellationToken);

        foreach (var setting in defaults)
        {
            if (existingKeys.Contains(setting.JobKey))
            {
                continue;
            }

            if (setting.Id == Guid.Empty)
            {
                setting.Id = Guid.NewGuid();
            }

            setting.UpdatedAtUtc = DateTime.UtcNow;
            await _dbContext.BackgroundJobSettings.AddAsync(setting, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> RecoverAbandonedRunsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var openRuns = await _dbContext.BackgroundJobRuns
            .Where(x => x.Status == BackgroundJobStatuses.Running)
            .ToListAsync(cancellationToken);

        foreach (var run in openRuns)
        {
            run.Status = BackgroundJobStatuses.Failed;
            run.Message = "Run abandoned (host restarted or worker lost).";
            run.CompletedAtUtc = now;
            run.DurationMs = (long)Math.Max(0, (now - run.StartedAtUtc).TotalMilliseconds);
        }

        var stuckSettings = await _dbContext.BackgroundJobSettings
            .Where(x => x.LastStatus == BackgroundJobStatuses.Running)
            .ToListAsync(cancellationToken);

        foreach (var setting in stuckSettings)
        {
            setting.LastStatus = BackgroundJobStatuses.Idle;
            setting.LastMessage = "Previous run was interrupted; scheduler reset on startup.";
            setting.LastRunCompletedAtUtc = now;
            setting.NextRunAtUtc = now.AddMinutes(1);
            setting.UpdatedAtUtc = now;
        }

        if (openRuns.Count == 0 && stuckSettings.Count == 0)
        {
            return 0;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return openRuns.Count;
    }

    public async Task<IReadOnlyList<string>> RecoverStaleActiveRunsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var openRuns = await _dbContext.BackgroundJobRuns
            .Where(x => x.Status == BackgroundJobStatuses.Running)
            .ToListAsync(cancellationToken);

        var recoveredJobKeys = new List<string>();

        foreach (var run in openRuns)
        {
            var maxAge = ResolveMaxRunAge(run.JobKey);
            if (now - run.StartedAtUtc < maxAge)
            {
                continue;
            }

            run.Status = BackgroundJobStatuses.Failed;
            run.Message = $"Timed out after {maxAge.TotalMinutes:0.#} minutes.";
            run.CompletedAtUtc = now;
            run.DurationMs = (long)Math.Max(0, (now - run.StartedAtUtc).TotalMilliseconds);
            recoveredJobKeys.Add(run.JobKey);
        }

        if (recoveredJobKeys.Count == 0)
        {
            return recoveredJobKeys;
        }

        var jobKeys = recoveredJobKeys.Distinct().ToList();
        var stuckSettings = await _dbContext.BackgroundJobSettings
            .Where(x => jobKeys.Contains(x.JobKey) && x.LastStatus == BackgroundJobStatuses.Running)
            .ToListAsync(cancellationToken);

        foreach (var setting in stuckSettings)
        {
            setting.LastStatus = BackgroundJobStatuses.Failed;
            setting.LastMessage = "Run exceeded maximum duration and was stopped.";
            setting.LastRunCompletedAtUtc = now;
            setting.NextRunAtUtc = now.AddMinutes(Math.Max(5, setting.IntervalMinutes));
            setting.UpdatedAtUtc = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return jobKeys;
    }

    private static TimeSpan ResolveMaxRunAge(string jobKey) =>
        jobKey switch
        {
            BackgroundJobKeys.InsightAlerts => TimeSpan.FromSeconds(30),
            BackgroundJobKeys.InsightSnapshots => TimeSpan.FromMinutes(45),
            _ => TimeSpan.FromMinutes(20)
        };
}
