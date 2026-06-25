using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class VariantUnitBarcodeRepository : IVariantUnitBarcodeRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public VariantUnitBarcodeRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<VariantUnitBarcode>> GetByBarcodesAsync(
        IEnumerable<string> barcodes,
        CancellationToken cancellationToken = default)
    {
        var normalized = barcodes
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0)
        {
            return Task.FromResult(new List<VariantUnitBarcode>());
        }

        var lowered = normalized.Select(x => x.ToLowerInvariant()).ToList();

        return _dbContext.VariantUnitBarcodes
            .Where(x => lowered.Contains(x.Barcode.ToLower()))
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<VariantUnitBarcode> Items, int TotalCount)> GetPagedListAsync(
        Guid productVariantId,
        Guid warehouseId,
        UnitBarcodeStatus? status,
        int skip,
        int take,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.VariantUnitBarcodes
            .Include(x => x.ProductVariant)
            .Include(x => x.Warehouse)
            .Where(x => x.ProductVariantId == productVariantId && x.WarehouseId == warehouseId)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var term = TextSearchHelper.Normalize(search);
        if (term is not null)
        {
            var pattern = TextSearchHelper.ToLikePattern(term);
            query = query.Where(x => EF.Functions.ILike(x.Barcode, pattern));
        }

        query = query.OrderBy(x => x.Barcode);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task InsertRangeAsync(
        IEnumerable<VariantUnitBarcode> items,
        CancellationToken cancellationToken = default) =>
        await _dbContext.VariantUnitBarcodes.AddRangeAsync(items, cancellationToken);

    public Task UpdateRangeAsync(
        IEnumerable<VariantUnitBarcode> items,
        CancellationToken cancellationToken = default)
    {
        _dbContext.VariantUnitBarcodes.UpdateRange(items);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
