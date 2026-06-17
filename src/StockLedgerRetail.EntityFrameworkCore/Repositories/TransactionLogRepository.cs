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

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
