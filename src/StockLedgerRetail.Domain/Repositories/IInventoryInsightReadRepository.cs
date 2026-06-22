namespace StockLedgerRetail.Domain.Repositories;

public interface IInventoryInsightReadRepository
{
    Task<List<DeadStockFact>> GetDeadStockFactsAsync(
        Guid? warehouseId,
        DateTime referenceDateUtc,
        int daysWithoutOutbound,
        decimal minOnHand,
        int maxResults,
        CancellationToken cancellationToken = default);

    Task<List<SalesVelocityFact>> GetSalesVelocityFactsAsync(
        Guid? warehouseId,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        int maxResults,
        CancellationToken cancellationToken = default);
}
