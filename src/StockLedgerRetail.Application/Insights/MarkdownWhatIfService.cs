using StockLedgerRetail.Insights;
using StockLedgerRetail.Pricing;

namespace StockLedgerRetail.Application.Insights;

public interface IMarkdownWhatIfService
{
    MarkdownWhatIfResultDto Simulate(MarkdownWhatIfRequestDto input);
}

public class MarkdownWhatIfService : IMarkdownWhatIfService
{
    public MarkdownWhatIfResultDto Simulate(MarkdownWhatIfRequestDto input)
    {
        var lines = new List<MarkdownWhatIfLineResultDto>();
        decimal totalRecovery = 0;
        decimal totalCostValue = 0;

        foreach (var line in input.Lines)
        {
            var percent = Math.Clamp(line.MarkdownPercent, 0, 95);
            var vatRate = line.RegularPriceBeforeVat.HasValue && input.VatRate.HasValue
                ? input.VatRate.Value
                : 10m;

            decimal? beforeVat = null;
            decimal? afterVat = null;
            if (line.RegularPriceBeforeVat.HasValue)
            {
                beforeVat = PricingCalculator.RoundCurrency(
                    line.RegularPriceBeforeVat.Value * (1 - percent / 100m));
                afterVat = PricingCalculator.CalcPriceAfterVat(beforeVat.Value, vatRate);
            }

            var recovery = afterVat.HasValue ? afterVat.Value * line.QuantityOnHand : 0;
            var costValue = (line.CostPrice ?? 0) * line.QuantityOnHand;
            decimal? marginPercent = null;
            if (beforeVat.HasValue && line.CostPrice.HasValue && beforeVat.Value > 0)
            {
                marginPercent = ((beforeVat.Value - line.CostPrice.Value) / beforeVat.Value) * 100;
            }

            totalRecovery += recovery;
            totalCostValue += costValue;

            lines.Add(new MarkdownWhatIfLineResultDto
            {
                ProductVariantId = line.ProductVariantId,
                Sku = line.Sku,
                QuantityOnHand = line.QuantityOnHand,
                MarkdownPercent = percent,
                PriceBeforeVatAfterMarkdown = beforeVat,
                PriceAfterVatAfterMarkdown = afterVat,
                RecoveryValueAfterVat = recovery > 0 ? recovery : null,
                GrossMarginPercentAfterMarkdown = marginPercent,
                InventoryValueAtCost = costValue > 0 ? costValue : null
            });
        }

        var capitalRelease = totalCostValue > 0
            ? Math.Round((totalRecovery / totalCostValue) * 100, 2)
            : 0;

        return new MarkdownWhatIfResultDto
        {
            Lines = lines,
            TotalRecoveryValueAfterVat = totalRecovery,
            TotalInventoryValueAtCost = totalCostValue,
            CapitalReleasePercent = capitalRelease
        };
    }
}
