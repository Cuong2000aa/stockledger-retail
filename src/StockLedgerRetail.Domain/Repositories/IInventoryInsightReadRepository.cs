namespace StockLedgerRetail.Domain.Repositories;

public interface IInventoryInsightReadRepository
{
    Task<List<DeadStockFact>> GetDeadStockFactsAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime referenceDateUtc,
        int daysWithoutOutbound,
        decimal minOnHand,
        int maxResults,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default);

    Task<List<SalesVelocityFact>> GetSalesVelocityFactsAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        int maxResults,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default);

    Task<List<MarkdownCandidateFact>> GetMarkdownCandidateFactsAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime referenceDateUtc,
        int daysWithoutOutbound,
        decimal minOnHand,
        int maxResults,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default);

    Task<List<PromotionRiskFact>> GetPromotionRiskFactsAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        int maxResults,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default);

    Task<List<ReorderRiskFact>> GetReorderRiskFactsAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        int maxResults,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default);

    Task<List<TrendSummaryFact>> GetTrendSummaryFactsAsync(
        Guid? warehouseId,
        Guid? brandId,
        string? regionCode,
        DateTime currentFromDateUtc,
        DateTime currentToDateUtc,
        DateTime previousFromDateUtc,
        DateTime previousToDateUtc,
        int maxResults,
        IReadOnlyCollection<Guid>? scopedWarehouseIds = null,
        CancellationToken cancellationToken = default);
}
