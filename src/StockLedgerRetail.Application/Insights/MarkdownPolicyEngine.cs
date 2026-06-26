using System.Text.Json;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Enums;
using StockLedgerRetail.MarkdownPolicies;

namespace StockLedgerRetail.Application.Insights;

public interface IMarkdownPolicyEngine
{
    MarkdownSuggestionDto? Evaluate(MarkdownEvaluationInput input, IReadOnlyList<MarkdownPolicy> policies);
}

public sealed record MarkdownEvaluationInput(
    Guid? BrandId,
    string? RegionCode,
    WarehouseType WarehouseType,
    int DaysWithoutOutbound,
    decimal QuantityOnHand,
    decimal? CostPrice,
    decimal? RegularPriceBeforeVat,
    decimal? VatRate,
    decimal ShopOutboundQuantity,
    decimal BrandMedianSellThrough);

public class MarkdownPolicyEngine : IMarkdownPolicyEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public MarkdownSuggestionDto? Evaluate(MarkdownEvaluationInput input, IReadOnlyList<MarkdownPolicy> policies)
    {
        if (!input.RegularPriceBeforeVat.HasValue || input.RegularPriceBeforeVat.Value <= 0)
        {
            return null;
        }

        var policy = ResolvePolicy(input.BrandId, input.RegionCode, input.WarehouseType, policies)
            ?? CreateSystemDefaultPolicy();

        if (input.DaysWithoutOutbound < policy.MinDaysWithoutOutbound
            || input.QuantityOnHand < policy.MinOnHand)
        {
            return null;
        }

        if (policy.MinInventoryValueAtCost.HasValue
            && input.CostPrice.HasValue
            && input.CostPrice.Value * input.QuantityOnHand < policy.MinInventoryValueAtCost.Value)
        {
            return null;
        }

        var tiers = DeserializeTiers(policy.TiersJson);
        if (tiers.Count == 0)
        {
            tiers = GetDefaultTiers();
        }

        var tier = tiers
            .Where(t => input.DaysWithoutOutbound >= t.MinDaysWithoutOutbound
                && (!t.MaxDaysWithoutOutbound.HasValue || input.DaysWithoutOutbound <= t.MaxDaysWithoutOutbound.Value))
            .OrderByDescending(t => t.MinDaysWithoutOutbound)
            .FirstOrDefault();

        if (tier is null)
        {
            return null;
        }

        var shopSellThrough = input.QuantityOnHand > 0
            ? input.ShopOutboundQuantity / input.QuantityOnHand
            : 0m;
        var brandMedian = input.BrandMedianSellThrough;

        var percent = tier.MarkdownPercent;
        if (brandMedian > 0
            && shopSellThrough < brandMedian * policy.SlowSellThroughThreshold)
        {
            percent = tier.SlowSellThroughMarkdownPercent ?? Math.Min(percent + 5m, policy.MaxMarkdownPercent);
        }

        percent = Math.Min(percent, policy.MaxMarkdownPercent);

        var regular = input.RegularPriceBeforeVat.Value;
        var candidateBefore = RoundMoney(regular * (1 - percent / 100m));

        if (input.CostPrice.HasValue && candidateBefore > 0)
        {
            var margin = (candidateBefore - input.CostPrice.Value) / candidateBefore * 100m;
            if (margin < policy.MinGrossMarginPercent)
            {
                var floorBefore = input.CostPrice.Value / (1 - policy.MinGrossMarginPercent / 100m);
                candidateBefore = RoundMoney(floorBefore);
                percent = regular > 0 ? RoundMoney((1 - candidateBefore / regular) * 100m) : 0m;
            }
        }

        if (!policy.AllowBelowCost && input.CostPrice.HasValue && candidateBefore < input.CostPrice.Value)
        {
            candidateBefore = RoundMoney(input.CostPrice.Value);
            percent = regular > 0 ? RoundMoney((1 - candidateBefore / regular) * 100m) : 0m;
        }

        percent = Math.Min(percent, policy.MaxMarkdownPercent);

        if (percent <= 0 || candidateBefore >= regular)
        {
            return null;
        }

        var candidateAfter = input.VatRate.HasValue
            ? RoundMoney(candidateBefore * (1 + input.VatRate.Value / 100m))
            : candidateBefore;

        var grossMarginAfter = input.CostPrice.HasValue && candidateBefore > 0
            ? RoundMoney((candidateBefore - input.CostPrice.Value) / candidateBefore * 100m)
            : (decimal?)null;

        var requiresApproval = policy.RequireApprovalAbovePercent.HasValue
            && percent > policy.RequireApprovalAbovePercent.Value;

        return new MarkdownSuggestionDto
        {
            PolicyId = policy.Id == Guid.Empty ? null : policy.Id,
            TierCode = tier.TierCode,
            MarkdownDepthPercent = percent,
            SuggestedMarkdownPriceBeforeVat = candidateBefore,
            SuggestedMarkdownPriceAfterVat = candidateAfter,
            GrossMarginAfterMarkdownPercent = grossMarginAfter,
            ShopSellThroughRatio = RoundMoney(shopSellThrough),
            BrandMedianSellThroughRatio = brandMedian > 0 ? RoundMoney(brandMedian) : null,
            Severity = tier.Severity,
            RuleCode = policy.Id == Guid.Empty ? "markdown_policy_default" : "markdown_policy",
            RequiresApproval = requiresApproval
        };
    }

    internal static MarkdownPolicy? ResolvePolicy(
        Guid? brandId,
        string? regionCode,
        WarehouseType warehouseType,
        IReadOnlyList<MarkdownPolicy> policies)
    {
        if (!brandId.HasValue)
        {
            return null;
        }

        var candidates = policies
            .Where(p => p.IsActive && p.BrandId == brandId.Value)
            .Where(p => string.IsNullOrWhiteSpace(p.RegionCode)
                || string.Equals(p.RegionCode, regionCode, StringComparison.OrdinalIgnoreCase))
            .Where(p => !p.WarehouseType.HasValue || p.WarehouseType.Value == warehouseType)
            .Select(p => new
            {
                Policy = p,
                Specificity = (string.IsNullOrWhiteSpace(p.RegionCode) ? 0 : 2)
                    + (p.WarehouseType.HasValue ? 1 : 0)
            })
            .OrderByDescending(x => x.Specificity)
            .FirstOrDefault();

        return candidates?.Policy;
    }

    internal static MarkdownPolicy CreateSystemDefaultPolicy() => new()
    {
        Id = Guid.Empty,
        BrandId = Guid.Empty,
        MinDaysWithoutOutbound = 60,
        MinOnHand = 1,
        MinGrossMarginPercent = 10,
        MaxMarkdownPercent = 50,
        SlowSellThroughThreshold = 0.5m,
        TiersJson = SerializeTiers(GetDefaultTiers()),
        IsActive = true
    };

    internal static List<MarkdownPolicyTierDto> GetDefaultTiers() =>
    [
        new MarkdownPolicyTierDto
        {
            TierCode = "watch",
            MinDaysWithoutOutbound = 60,
            MaxDaysWithoutOutbound = 89,
            MarkdownPercent = 10,
            SlowSellThroughMarkdownPercent = 15,
            Severity = "warning"
        },
        new MarkdownPolicyTierDto
        {
            TierCode = "moderate",
            MinDaysWithoutOutbound = 90,
            MaxDaysWithoutOutbound = 119,
            MarkdownPercent = 15,
            SlowSellThroughMarkdownPercent = 20,
            Severity = "warning"
        },
        new MarkdownPolicyTierDto
        {
            TierCode = "aggressive",
            MinDaysWithoutOutbound = 120,
            MarkdownPercent = 25,
            SlowSellThroughMarkdownPercent = 30,
            Severity = "critical"
        }
    ];

    internal static List<MarkdownPolicyTierDto> DeserializeTiers(string? tiersJson)
    {
        if (string.IsNullOrWhiteSpace(tiersJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<MarkdownPolicyTierDto>>(tiersJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    internal static string SerializeTiers(IReadOnlyList<MarkdownPolicyTierDto> tiers) =>
        JsonSerializer.Serialize(tiers, JsonOptions);

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 0, MidpointRounding.AwayFromZero);
}
