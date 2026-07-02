using StockLedgerRetail.Insights;

namespace StockLedgerRetail.Application.Insights;

public interface IInsightExplainService
{
    InsightExplainResponseDto Explain(InsightExplainRequestDto input);
}

public class InsightExplainService : IInsightExplainService
{
    public InsightExplainResponseDto Explain(InsightExplainRequestDto input)
    {
        var location = BuildLocation(input);
        var summary = $"Insight [{input.InsightKind}] for {location} — priority {input.Priority}, action {input.ActionCode}.";

        var rationale = ResolveRationale(input.ActionCode);
        var paragraphs = new List<string> { summary };
        if (!string.IsNullOrWhiteSpace(rationale))
        {
            paragraphs.Add(rationale);
        }

        var evidenceLines = input.Evidence
            .Where(x => x.Key is not "sku" and not "warehouseCode")
            .Select(x => $"{x.Key}: {x.Value}")
            .ToList();

        var nextSteps = ResolveNextSteps(input.ActionCode);

        return new InsightExplainResponseDto
        {
            Summary = summary,
            RationaleParagraphs = paragraphs,
            EvidenceLines = evidenceLines,
            SuggestedNextSteps = nextSteps
        };
    }

    private static string BuildLocation(InsightExplainRequestDto input)
    {
        if (!string.IsNullOrWhiteSpace(input.WarehouseCode) && !string.IsNullOrWhiteSpace(input.Sku))
        {
            return $"SKU {input.Sku} at {input.WarehouseCode}";
        }

        if (!string.IsNullOrWhiteSpace(input.SourceWarehouseCode)
            && !string.IsNullOrWhiteSpace(input.DestinationWarehouseCode))
        {
            return $"transfer {input.Sku ?? "SKU"} from {input.SourceWarehouseCode} to {input.DestinationWarehouseCode}";
        }

        return input.Sku ?? "item";
    }

    private static string? ResolveRationale(string actionCode)
    {
        if (actionCode.StartsWith("dead_stock_critical", StringComparison.Ordinal))
        {
            return "Stock has been idle too long with significant tied capital. Markdown or move to clearance is recommended.";
        }

        if (actionCode.StartsWith("velocity_replenish_urgent", StringComparison.Ordinal))
        {
            return "Sell-through is strong and cover days are low. Replenish before stockout hurts revenue.";
        }

        if (actionCode.StartsWith("transfer_execute", StringComparison.Ordinal))
        {
            return "Source warehouse has surplus while destination is under-covered based on recent outbound.";
        }

        if (actionCode.StartsWith("broken_size_run", StringComparison.Ordinal))
        {
            return "Incomplete size curve reduces sell-through. Consolidate sizes or transfer to stores that can complete the run.";
        }

        if (actionCode.StartsWith("season_clearance", StringComparison.Ordinal))
        {
            return "SKU belongs to a past season with slow movement. Season-end clearance protects margin and frees capital.";
        }

        if (actionCode.StartsWith("reorder_risk", StringComparison.Ordinal))
        {
            return "Available stock and inbound pipeline are insufficient for current demand velocity.";
        }

        return null;
    }

    private static List<string> ResolveNextSteps(string actionCode)
    {
        if (actionCode.StartsWith("transfer", StringComparison.Ordinal)
            || actionCode.StartsWith("broken_size", StringComparison.Ordinal))
        {
            return ["Review transfer suggestion", "Create draft transfer document", "Track execution in insight actions"];
        }

        if (actionCode.Contains("markdown", StringComparison.Ordinal)
            || actionCode.StartsWith("season_clearance", StringComparison.Ordinal))
        {
            return ["Run markdown what-if", "Apply markdown price on SKU", "Monitor sell-through after price change"];
        }

        if (actionCode.StartsWith("reorder", StringComparison.Ordinal)
            || actionCode.StartsWith("velocity_replenish", StringComparison.Ordinal))
        {
            return ["Draft purchase order", "Confirm inbound GR schedule", "Monitor cover days"];
        }

        return ["Open related stock history", "Review SKU master data"];
    }
}
