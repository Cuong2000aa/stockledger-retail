using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

/// <summary>
/// Đọc báo cáo bằng SQL aggregate — không load full bảng vào memory.
/// SQL nằm trong <see cref="InventoryReportSqlQueries"/> để copy sang pgAdmin khi debug.
/// </summary>
public class InventoryReportReadRepository : IInventoryReportReadRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;
    private readonly ILogger<InventoryReportReadRepository> _logger;

    public InventoryReportReadRepository(
        StockLedgerRetailDbContext dbContext,
        ILogger<InventoryReportReadRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(decimal TotalValue, int TotalLineCount)> GetInventoryValueTotalsAsync(
        Guid? warehouseId,
        Guid? brandId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "SQL inventory value totals: warehouseId={WarehouseId}, brandId={BrandId}",
            warehouseId,
            brandId);

        var result = await _dbContext.Database
            .SqlQueryRaw<InventoryValueTotalsRow>(
                InventoryReportSqlQueries.InventoryValueTotals,
                warehouseId,
                brandId)
            .FirstAsync(cancellationToken);

        _logger.LogDebug(
            "SQL inventory value totals result: totalValue={TotalValue}, lineCount={LineCount}",
            result.TotalValue,
            result.TotalLineCount);

        return (result.TotalValue, result.TotalLineCount);
    }

    public async Task<List<InventoryValueLineReadModel>> GetInventoryValueLinesAsync(
        Guid? warehouseId,
        Guid? brandId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "SQL inventory value lines: warehouseId={WarehouseId}, brandId={BrandId}, skip={Skip}, take={Take}",
            warehouseId,
            brandId,
            skip,
            take);

        var lines = await _dbContext.Database
            .SqlQueryRaw<InventoryValueLineReadModel>(
                InventoryReportSqlQueries.InventoryValueLines,
                warehouseId,
                brandId,
                skip,
                take)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("SQL inventory value lines returned {RowCount} row(s).", lines.Count);
        return lines;
    }

    public async Task<NxtReportTotalsReadModel> GetNxtTotalsAsync(
        DateTime fromInclusive,
        DateTime toExclusive,
        Guid? warehouseId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "SQL NXT totals: from={FromInclusive}, toExclusive={ToExclusive}, warehouseId={WarehouseId}",
            fromInclusive,
            toExclusive,
            warehouseId);

        var totals = await _dbContext.Database
            .SqlQueryRaw<NxtReportTotalsReadModel>(
                InventoryReportSqlQueries.NxtTotals,
                fromInclusive,
                toExclusive,
                warehouseId)
            .FirstAsync(cancellationToken);

        _logger.LogDebug(
            "SQL NXT totals: lineCount={LineCount}, closingValue={ClosingValue}",
            totals.TotalLineCount,
            totals.TotalClosingValue);

        return totals;
    }

    public async Task<List<NxtMovementLineReadModel>> GetNxtLinesAsync(
        DateTime fromInclusive,
        DateTime toExclusive,
        Guid? warehouseId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "SQL NXT lines: from={FromInclusive}, toExclusive={ToExclusive}, warehouseId={WarehouseId}, skip={Skip}, take={Take}",
            fromInclusive,
            toExclusive,
            warehouseId,
            skip,
            take);

        var lines = await _dbContext.Database
            .SqlQueryRaw<NxtMovementLineReadModel>(
                InventoryReportSqlQueries.NxtLines,
                fromInclusive,
                toExclusive,
                warehouseId,
                skip,
                take)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("SQL NXT lines returned {RowCount} row(s).", lines.Count);
        return lines;
    }

    private sealed class InventoryValueTotalsRow
    {
        public decimal TotalValue { get; set; }

        public int TotalLineCount { get; set; }
    }
}
