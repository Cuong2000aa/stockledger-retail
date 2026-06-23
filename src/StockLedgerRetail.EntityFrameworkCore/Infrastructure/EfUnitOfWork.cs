using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Infrastructure;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public EfUnitOfWork(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default) =>
        ExecuteCore(action, cancellationToken);

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        await ExecuteCore(
            async ct =>
            {
                await action(ct);
                return true;
            },
            cancellationToken);
    }

    private async Task<T> ExecuteCore<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            var nestedResult = await action(cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return nestedResult;
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var result = await action(cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new InvalidOperationException(
                    "Data was modified by another process. Refresh and try again.",
                    ex);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
