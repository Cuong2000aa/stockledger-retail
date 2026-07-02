using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Domain.Repositories;

public interface IBackgroundJobRepository
{
    Task<List<BackgroundJobSetting>> GetSettingsAsync(CancellationToken cancellationToken = default);

    Task<BackgroundJobSetting?> GetSettingByKeyAsync(string jobKey, CancellationToken cancellationToken = default);

    Task UpdateSettingAsync(BackgroundJobSetting setting, CancellationToken cancellationToken = default);

    Task InsertRunAsync(BackgroundJobRun run, CancellationToken cancellationToken = default);

    Task UpdateRunAsync(BackgroundJobRun run, CancellationToken cancellationToken = default);

    Task<List<BackgroundJobRun>> GetRecentRunsAsync(
        string? jobKey,
        int limit,
        CancellationToken cancellationToken = default);

    Task EnsureDefaultSettingsAsync(
        IReadOnlyList<BackgroundJobSetting> defaults,
        CancellationToken cancellationToken = default);

    Task<int> RecoverAbandonedRunsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> RecoverStaleActiveRunsAsync(CancellationToken cancellationToken = default);
}
