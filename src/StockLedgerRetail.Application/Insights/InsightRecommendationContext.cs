using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Insights;

namespace StockLedgerRetail.Application.Insights;

public sealed class InsightRecommendationContext
{
    public required IReadOnlyList<Warehouse> Warehouses { get; init; }

    public required IReadOnlyList<TransferPolicy> TransferPolicies { get; init; }

    public required IReadOnlyList<MarkdownPolicy> MarkdownPolicies { get; init; }

    public required IReadOnlyDictionary<string, TransferSuggestionDto> ReplenishTransferByDestinationKey { get; init; }

    public static string BuildReplenishKey(Guid productVariantId, Guid destinationWarehouseId) =>
        $"{productVariantId:N}:{destinationWarehouseId:N}";
}
