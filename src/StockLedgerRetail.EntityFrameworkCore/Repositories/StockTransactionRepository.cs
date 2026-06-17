using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class StockTransactionRepository : IStockTransactionRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public StockTransactionRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task InsertAsync(StockTransaction transaction, CancellationToken cancellationToken = default) =>
        await _dbContext.StockTransactions.AddAsync(transaction, cancellationToken);

    public Task<int> CountByDatePrefixAsync(string datePrefix, CancellationToken cancellationToken = default) =>
        _dbContext.StockTransactions.CountAsync(x => x.TransactionNo.StartsWith(datePrefix), cancellationToken);

    public Task<List<StockTransaction>> GetListAsync(
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.StockTransactions
            .Include(x => x.ProductVariant)
            .Include(x => x.Warehouse)
            .AsQueryable();

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        }

        if (productVariantId.HasValue)
        {
            query = query.Where(x => x.ProductVariantId == productVariantId.Value);
        }

        return query
            .OrderByDescending(x => x.TransactionDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
