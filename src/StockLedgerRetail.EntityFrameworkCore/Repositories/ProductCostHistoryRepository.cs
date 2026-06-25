using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class ProductCostHistoryRepository : IProductCostHistoryRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public ProductCostHistoryRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProductCostHistory?> GetActiveByVariantAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default) =>
        _dbContext.ProductCostHistories
            .Where(x => x.ProductVariantId == productVariantId && x.IsCurrent)
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<(List<ProductCostHistory> Items, int TotalCount)> GetPagedListAsync(
        Guid? productVariantId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ProductCostHistories
            .Include(x => x.ProductVariant)
            .AsNoTracking();

        if (productVariantId.HasValue)
        {
            query = query.Where(x => x.ProductVariantId == productVariantId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.EffectiveFrom)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task InsertAsync(ProductCostHistory history, CancellationToken cancellationToken = default) =>
        await _dbContext.ProductCostHistories.AddAsync(history, cancellationToken);

    public Task UpdateAsync(ProductCostHistory history, CancellationToken cancellationToken = default)
    {
        _dbContext.ProductCostHistories.Update(history);
        return Task.CompletedTask;
    }
}
