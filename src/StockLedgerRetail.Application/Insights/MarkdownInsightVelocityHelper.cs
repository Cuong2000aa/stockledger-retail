using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.Application.Insights;

internal static class MarkdownInsightVelocityHelper
{
    public static string BuildVelocityKey(Guid productVariantId, Guid warehouseId) =>
        $"{productVariantId:N}:{warehouseId:N}";

    public static Dictionary<string, SalesVelocityFact> IndexVelocityFacts(
        IReadOnlyList<SalesVelocityFact> velocityFacts) =>
        velocityFacts.ToDictionary(x => BuildVelocityKey(x.ProductVariantId, x.WarehouseId));

    public static Dictionary<string, decimal> BuildBrandMedianSellThrough(
        IReadOnlyList<SalesVelocityFact> velocityFacts)
    {
        return velocityFacts
            .GroupBy(x => x.BrandId?.ToString() ?? "none")
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var ratios = g
                        .Select(x => x.QuantityOnHand > 0 ? x.OutboundQuantity / x.QuantityOnHand : 0m)
                        .Where(r => r > 0)
                        .OrderBy(r => r)
                        .ToList();

                    if (ratios.Count == 0)
                    {
                        return 0m;
                    }

                    var mid = ratios.Count / 2;
                    return ratios.Count % 2 == 0
                        ? (ratios[mid - 1] + ratios[mid]) / 2m
                        : ratios[mid];
                });
    }
}
