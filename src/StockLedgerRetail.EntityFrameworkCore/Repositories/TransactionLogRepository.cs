using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Repositories;

public class TransactionLogRepository : ITransactionLogRepository
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public TransactionLogRepository(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task InsertAsync(TransactionLog transactionLog, CancellationToken cancellationToken = default) =>
        await _dbContext.TransactionLogs.AddAsync(transactionLog, cancellationToken);

    public async Task<(List<TransactionLog> Items, int TotalCount)> GetPagedListAsync(
        string? entityName,
        Guid? entityId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TransactionLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(x => x.EntityName == entityName);
        }

        if (entityId.HasValue)
        {
            query = query.Where(x => x.EntityId == entityId.Value);
        }

        query = query.OrderByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
