using System.Text.Json;
using StockLedgerRetail.Pricing;
using Xunit;

namespace StockLedgerRetail.Domain.Shared.Tests;

public class PricingCalculatorTests
{
    [Theory]
    [InlineData(100, 0, 100)]
    [InlineData(100, 8, 108)]
    [InlineData(100, 10, 110)]
    [InlineData(99.99, 10, 109.989)]
    public void CalcPriceAfterVat_matches_contract(decimal before, decimal vat, decimal expectedAfter)
    {
        Assert.Equal(expectedAfter, PricingCalculator.CalcPriceAfterVat(before, vat));
    }

    [Theory]
    [InlineData(108, 8, 100)]
    [InlineData(110, 10, 100)]
    public void CalcPriceBeforeVat_matches_contract(decimal after, decimal vat, decimal expectedBefore)
    {
        Assert.Equal(expectedBefore, PricingCalculator.CalcPriceBeforeVat(after, vat));
    }

    [Fact]
    public void NormalizeSellingPrice_rejects_mismatched_before_and_after()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PricingCalculator.NormalizeSellingPrice(null, 100m, 120m, 10m));
    }

    [Fact]
    public void Shared_contract_cases_match_backend_calculator()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "pricing-contract-cases.json");
        var json = File.ReadAllText(path);
        var cases = JsonSerializer.Deserialize<List<ContractCase>>(json)
            ?? throw new InvalidOperationException("Contract cases missing.");

        foreach (var testCase in cases)
        {
            if (testCase.PriceBeforeVat.HasValue)
            {
                Assert.Equal(
                    testCase.PriceAfterVat,
                    PricingCalculator.CalcPriceAfterVat(testCase.PriceBeforeVat.Value, testCase.VatRate));
            }
            else if (testCase.PriceAfterVat.HasValue)
            {
                Assert.Equal(
                    testCase.PriceBeforeVat,
                    PricingCalculator.CalcPriceBeforeVat(testCase.PriceAfterVat.Value, testCase.VatRate));
            }
        }
    }

    private sealed class ContractCase
    {
        public decimal? PriceBeforeVat { get; set; }

        public decimal VatRate { get; set; }

        public decimal? PriceAfterVat { get; set; }
    }
}
