using Microsoft.EntityFrameworkCore;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.EntityFrameworkCore.Infrastructure;

public class DocumentNumberGenerator : IDocumentNumberGenerator
{
    private readonly StockLedgerRetailDbContext _dbContext;
    private readonly Dictionary<string, int> _nextSequenceByPrefix = new(StringComparer.Ordinal);

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

        if (!_nextSequenceByPrefix.TryGetValue(prefix, out var sequence))
        {
            var count = await countByPrefixAsync(prefix, cancellationToken);
            sequence = count + 1;
        }

        _nextSequenceByPrefix[prefix] = sequence + 1;
        return $"{prefix}{sequence.ToString().PadLeft(sequencePadLength, '0')}";
    }
}
