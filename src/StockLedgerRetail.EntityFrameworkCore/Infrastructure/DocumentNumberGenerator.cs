using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Infrastructure;

public class DocumentNumberGenerator : IDocumentNumberGenerator
{
    private readonly StockLedgerRetailDbContext _dbContext;

    public DocumentNumberGenerator(StockLedgerRetailDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> NextAsync(
        string prefix,
        Func<string, CancellationToken, Task<int>> countByPrefixAsync,
        int sequencePadLength,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({prefix}))",
            cancellationToken);

        var count = await countByPrefixAsync(prefix, cancellationToken);
        return $"{prefix}{(count + 1).ToString().PadLeft(sequencePadLength, '0')}";
    }
}
