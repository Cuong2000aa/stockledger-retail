namespace StockLedgerRetail.Domain.Repositories;

public interface IDocumentNumberGenerator
{
    Task<string> NextAsync(
        string prefix,
        Func<string, CancellationToken, Task<int>> countByPrefixAsync,
        int sequencePadLength,
        CancellationToken cancellationToken = default);
}
