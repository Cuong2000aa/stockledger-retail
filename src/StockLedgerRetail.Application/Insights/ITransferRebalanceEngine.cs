using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Insights;

namespace StockLedgerRetail.Application.Insights;

public sealed class TransferRebalanceRequest
{
    public Guid? SourceWarehouseId { get; init; }

    public Guid? DestinationWarehouseId { get; init; }

    public int LookbackDays { get; init; } = 30;

    public int TargetCoverDays { get; init; } = 14;

    public int ReserveCoverDays { get; init; } = 7;

    public int MaxResults { get; init; } = 20;
}

public interface ITransferRebalanceEngine
{
    /// <summary>
    /// Builds transfer suggestions from velocity facts. Does not attach Recommendation CTAs.
    /// </summary>
    List<TransferSuggestionDto> Suggest(
        IReadOnlyList<SalesVelocityFact> facts,
        IReadOnlyList<TransferPolicy> policies,
        TransferRebalanceRequest request);
}
