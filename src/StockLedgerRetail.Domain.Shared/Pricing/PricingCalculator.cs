namespace StockLedgerRetail.Pricing;

/// <summary>Single source of truth for VAT price math (mirrored on the frontend for preview only).</summary>
public static class PricingCalculator
{
    public const decimal MismatchTolerance = 0.0001m;

    public static decimal RoundCurrency(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);

    public static decimal CalcPriceAfterVat(decimal priceBeforeVat, decimal vatRate) =>
        RoundCurrency(priceBeforeVat * (1 + vatRate / 100m));

    public static decimal CalcPriceBeforeVat(decimal priceAfterVat, decimal vatRate)
    {
        var divisor = 1 + vatRate / 100m;
        return divisor == 0 ? priceAfterVat : RoundCurrency(priceAfterVat / divisor);
    }

    public static bool PricesMatchVat(decimal priceBeforeVat, decimal priceAfterVat, decimal vatRate)
    {
        var expectedAfter = CalcPriceAfterVat(priceBeforeVat, vatRate);
        return Math.Abs(expectedAfter - priceAfterVat) <= MismatchTolerance;
    }

    public sealed record NormalizedSellingPrice(
        decimal? PriceBeforeVat,
        decimal? PriceAfterVat,
        decimal? CurrentSellingPrice,
        decimal? VatRate);

    /// <summary>Before VAT is authoritative when both sides are supplied.</summary>
    public static NormalizedSellingPrice NormalizeSellingPrice(
        decimal? sellingPrice,
        decimal? sellingPriceBeforeVat,
        decimal? sellingPriceAfterVat,
        decimal? vatRate)
    {
        var normalizedVatRate = vatRate ?? 0m;
        var before = sellingPriceBeforeVat;
        var after = sellingPriceAfterVat ?? sellingPrice;

        if (before.HasValue)
        {
            var derivedAfter = CalcPriceAfterVat(before.Value, normalizedVatRate);
            if (after.HasValue && !PricesMatchVat(before.Value, after.Value, normalizedVatRate))
            {
                throw new InvalidOperationException(
                    "Selling price before VAT and after VAT do not match the configured VAT rate.");
            }

            after = derivedAfter;
        }
        else if (after.HasValue)
        {
            before = CalcPriceBeforeVat(after.Value, normalizedVatRate);
        }

        return new NormalizedSellingPrice(before, after, after, vatRate);
    }

    public static (decimal MarginValue, decimal MarginRatePercent) CalcMarginBeforeVat(
        decimal? costPrice,
        decimal? sellingPriceBeforeVat)
    {
        if (!costPrice.HasValue || !sellingPriceBeforeVat.HasValue)
        {
            return (0m, 0m);
        }

        var marginValue = sellingPriceBeforeVat.Value - costPrice.Value;
        var marginRate = sellingPriceBeforeVat.Value == 0
            ? 0m
            : RoundCurrency((marginValue / sellingPriceBeforeVat.Value) * 100m);

        return (marginValue, marginRate);
    }
}
