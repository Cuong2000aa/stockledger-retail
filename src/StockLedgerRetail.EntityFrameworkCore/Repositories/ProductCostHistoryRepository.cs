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
            .Where(x => x.ProductVariantId == productVariantId && x.EffectiveTo == null)
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task InsertAsync(ProductCostHistory history, CancellationToken cancellationToken = default) =>
        await _dbContext.ProductCostHistories.AddAsync(history, cancellationToken);

    public Task UpdateAsync(ProductCostHistory history, CancellationToken cancellationToken = default)
    {
        _dbContext.ProductCostHistories.Update(history);
        return Task.CompletedTask;
    }
}
