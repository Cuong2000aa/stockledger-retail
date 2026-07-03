using Microsoft.Extensions.Logging;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.Application.Analytics;

public interface IInventoryDailyRollupService
{
    Task RefreshAsync(DateOnly? snapshotDate = null, CancellationToken cancellationToken = default);
}

public class InventoryDailyRollupService : IInventoryDailyRollupService
{
    private readonly IInventoryDailyRollupRepository _rollupRepository;
    private readonly ILogger<InventoryDailyRollupService> _logger;

    public InventoryDailyRollupService(
        IInventoryDailyRollupRepository rollupRepository,
        ILogger<InventoryDailyRollupService> logger)
    {
        _rollupRepository = rollupRepository;
        _logger = logger;
    }

    public async Task RefreshAsync(DateOnly? snapshotDate = null, CancellationToken cancellationToken = default)
    {
        var targetDate = snapshotDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var rollups = await _rollupRepository.BuildRollupsForDateAsync(targetDate, cancellationToken);
        await _rollupRepository.ReplaceForDateAsync(targetDate, rollups, cancellationToken);

        _logger.LogInformation(
            "Inventory daily rollups refreshed for {SnapshotDate} with {RowCount} rows.",
            targetDate,
            rollups.Count);
    }
}
