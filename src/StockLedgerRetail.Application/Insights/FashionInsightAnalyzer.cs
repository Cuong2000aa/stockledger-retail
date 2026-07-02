using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Insights;

namespace StockLedgerRetail.Application.Insights;

public static class FashionInsightAnalyzer
{
    public static List<BrokenSizeRunInsightDto> BuildBrokenSizeRuns(
        IReadOnlyList<FashionStockFact> facts,
        int maxResults)
    {
        var groups = facts
            .Where(x => !string.IsNullOrWhiteSpace(x.Size))
            .GroupBy(x => new
            {
                x.ProductId,
                ColorKey = NormalizeKey(x.Color),
                x.WarehouseId
            })
            .Where(g => g.Count() >= 2)
            .Select(g =>
            {
                var variants = g.ToList();
                var sizesWithStock = variants.Where(v => v.QuantityOnHand > 0).ToList();
                var sizesWithoutStock = variants.Where(v => v.QuantityOnHand <= 0).ToList();
                return new
                {
                    g.Key.ProductId,
                    g.Key.ColorKey,
                    g.Key.WarehouseId,
                    Variants = variants,
                    SizesWithStock = sizesWithStock,
                    SizesWithoutStock = sizesWithoutStock
                };
            })
            .Where(x => x.SizesWithStock.Count > 0 && x.SizesWithoutStock.Count > 0)
            .OrderByDescending(x => x.SizesWithoutStock.Count)
            .ThenByDescending(x => x.SizesWithStock.Sum(v => v.QuantityOnHand))
            .Take(maxResults)
            .ToList();

        return groups.Select(x =>
        {
            var first = x.Variants[0];
            var totalOnHand = x.SizesWithStock.Sum(v => v.QuantityOnHand);
            var inventoryValue = x.SizesWithStock.Sum(v => v.InventoryValue ?? 0);
            var missingRatio = (decimal)x.SizesWithoutStock.Count / x.Variants.Count;

            return new BrokenSizeRunInsightDto
            {
                ProductId = x.ProductId,
                ProductName = first.ProductName,
                Color = first.Color,
                WarehouseId = x.WarehouseId,
                WarehouseCode = first.WarehouseCode,
                WarehouseName = first.WarehouseName,
                BrandId = first.BrandId,
                TotalSizesInRun = x.Variants.Count,
                SizesWithStock = x.SizesWithStock.Count,
                SizesWithoutStock = x.SizesWithoutStock.Count,
                SizesInStock = x.SizesWithStock.Select(v => v.Size!).Distinct().OrderBy(s => s).ToList(),
                MissingSizes = x.SizesWithoutStock.Select(v => v.Size!).Distinct().OrderBy(s => s).ToList(),
                Variants = x.SizesWithStock.Select(v => new BrokenSizeRunVariantDto
                {
                    ProductVariantId = v.ProductVariantId,
                    Sku = v.Sku,
                    Size = v.Size,
                    QuantityOnHand = v.QuantityOnHand
                }).ToList(),
                TotalOnHand = totalOnHand,
                InventoryValue = inventoryValue > 0 ? inventoryValue : null,
                Severity = missingRatio >= 0.5m || x.SizesWithStock.Count == 1 ? "critical" : "warning"
            };
        }).ToList();
    }

    public static List<SeasonClearanceInsightDto> BuildSeasonClearance(
        IReadOnlyList<FashionStockFact> facts,
        string? currentSeason,
        int daysWithoutOutbound,
        int maxResults)
    {
        var normalizedCurrent = NormalizeSeason(currentSeason);
        var referenceDate = DateTime.UtcNow.Date;

        return facts
            .Where(x =>
                x.QuantityOnHand > 0
                && !string.IsNullOrWhiteSpace(x.Season)
                && IsPastSeason(x.Season, normalizedCurrent))
            .Select(x =>
            {
                var daysIdle = x.LastOutboundAt.HasValue
                    ? Math.Max(0, (referenceDate - x.LastOutboundAt.Value.Date).Days)
                    : daysWithoutOutbound;

                return new SeasonClearanceInsightDto
                {
                    ProductVariantId = x.ProductVariantId,
                    Sku = x.Sku,
                    ProductName = x.ProductName,
                    Season = x.Season,
                    WarehouseId = x.WarehouseId,
                    WarehouseCode = x.WarehouseCode,
                    WarehouseName = x.WarehouseName,
                    BrandId = x.BrandId,
                    QuantityOnHand = x.QuantityOnHand,
                    OutboundQuantity = x.OutboundQuantity,
                    DaysWithoutOutbound = daysIdle,
                    InventoryValue = x.InventoryValue,
                    Severity = daysIdle >= daysWithoutOutbound ? "critical" : "warning"
                };
            })
            .Where(x => x.DaysWithoutOutbound >= Math.Max(30, daysWithoutOutbound / 2))
            .OrderByDescending(x => x.InventoryValue ?? 0)
            .ThenByDescending(x => x.DaysWithoutOutbound)
            .Take(maxResults)
            .ToList();
    }

    private static string NormalizeKey(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "_" : value.Trim().ToUpperInvariant();

    private static string? NormalizeSeason(string? season) =>
        string.IsNullOrWhiteSpace(season) ? null : season.Trim().ToUpperInvariant();

    private static bool IsPastSeason(string? variantSeason, string? currentSeason)
    {
        var normalized = NormalizeSeason(variantSeason);
        if (normalized is null)
        {
            return false;
        }

        if (currentSeason is null)
        {
            return true;
        }

        return !string.Equals(normalized, currentSeason, StringComparison.OrdinalIgnoreCase);
    }
}
