using System.Text.Json;
using StockLedgerRetail.Application.Insights;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Insights;

namespace StockLedgerRetail.Application.Insights;

public interface IGlobalExecutiveSummaryAggregator
{
    Task<InsightsExecutiveSummaryDto?> TryGetAggregatedAsync(
        int lookbackDays,
        int daysWithoutOutbound,
        CancellationToken cancellationToken = default);

    Task RefreshAsync(
        int lookbackDays,
        int daysWithoutOutbound,
        CancellationToken cancellationToken = default);
}

public class GlobalExecutiveSummaryAggregator : IGlobalExecutiveSummaryAggregator
{
    private readonly IInsightSnapshotRepository _insightSnapshotRepository;

    public GlobalExecutiveSummaryAggregator(IInsightSnapshotRepository insightSnapshotRepository)
    {
        _insightSnapshotRepository = insightSnapshotRepository;
    }

    public async Task<InsightsExecutiveSummaryDto?> TryGetAggregatedAsync(
        int lookbackDays,
        int daysWithoutOutbound,
        CancellationToken cancellationToken = default)
    {
        var brandSnapshots = await _insightSnapshotRepository.GetBrandExecutiveSummariesAsync(
            lookbackDays,
            daysWithoutOutbound,
            cancellationToken);

        return AggregateSummaries(brandSnapshots);
    }

    public async Task RefreshAsync(
        int lookbackDays,
        int daysWithoutOutbound,
        CancellationToken cancellationToken = default)
    {
        var brandSnapshots = await _insightSnapshotRepository.GetBrandExecutiveSummariesAsync(
            lookbackDays,
            daysWithoutOutbound,
            cancellationToken);

        var aggregated = AggregateSummaries(brandSnapshots);
        if (aggregated is null)
        {
            return;
        }

        var snapshotKey = InsightSnapshotKeyBuilder.BuildExecutiveSummaryKey(
            null,
            null,
            null,
            lookbackDays,
            daysWithoutOutbound);

        await _insightSnapshotRepository.UpsertAsync(new InsightSnapshot
        {
            SnapshotKey = snapshotKey,
            InsightKind = InsightSnapshotKeyBuilder.KindExecutiveSummary,
            PayloadJson = JsonSerializer.Serialize(aggregated),
            GeneratedAtUtc = DateTime.UtcNow
        }, cancellationToken);
    }

    private static InsightsExecutiveSummaryDto? AggregateSummaries(IReadOnlyList<InsightSnapshot> brandSnapshots)
    {
        if (brandSnapshots.Count == 0)
        {
            return null;
        }

        var summaries = brandSnapshots
            .Select(DeserializeSummary)
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();

        if (summaries.Count == 0)
        {
            return null;
        }

        return new InsightsExecutiveSummaryDto
        {
            DeadStockCount = summaries.Sum(x => x.DeadStockCount),
            TiedCapital = summaries.Sum(x => x.TiedCapital),
            InventoryValueAtRisk = summaries.Sum(x => x.InventoryValueAtRisk),
            MarginAtRisk = summaries.Sum(x => x.MarginAtRisk),
            PromotionRiskCount = summaries.Sum(x => x.PromotionRiskCount),
            ReorderRiskCount = summaries.Sum(x => x.ReorderRiskCount),
            TransferOpportunityCount = summaries.Sum(x => x.TransferOpportunityCount),
            TransferOpportunityValue = summaries.Sum(x => x.TransferOpportunityValue),
            MarkdownCandidateCount = summaries.Sum(x => x.MarkdownCandidateCount),
            MarkdownRecoveryValue = summaries.Sum(x => x.MarkdownRecoveryValue)
        };
    }

    private static InsightsExecutiveSummaryDto? DeserializeSummary(InsightSnapshot snapshot)
    {
        try
        {
            return JsonSerializer.Deserialize<InsightsExecutiveSummaryDto>(snapshot.PayloadJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
