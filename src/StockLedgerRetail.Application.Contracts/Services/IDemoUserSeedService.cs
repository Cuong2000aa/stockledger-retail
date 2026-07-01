namespace StockLedgerRetail.Services;

public interface IDemoUserSeedService
{
    Task EnsureDemoClerkAsync(CancellationToken cancellationToken = default);
}
