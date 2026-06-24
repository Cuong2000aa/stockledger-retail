using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

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
}
