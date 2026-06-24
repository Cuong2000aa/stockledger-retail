namespace StockLedgerRetail.Services;

public interface IFbDataSeedService
{
    Task EnsureSeedAsync(CancellationToken cancellationToken = default);
}
