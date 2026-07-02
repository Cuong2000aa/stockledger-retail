using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Insights;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Insights;

public interface IInsightAlertService
{
    Task RunAsync(CancellationToken cancellationToken = default);
}

public class InsightAlertOptions
{
    public const string SectionName = "InsightAlerts";

    public bool Enabled { get; set; } = true;

    public int DeadStockCountThreshold { get; set; } = 10;

    public decimal TiedCapitalThreshold { get; set; } = 50_000_000;

    public int ReorderRiskCountThreshold { get; set; } = 5;
}

public class InsightAlertService : IInsightAlertService
{
    private readonly IInventoryInsightsAppService _inventoryInsightsAppService;
    private readonly IInsightActionAppService _insightActionAppService;
    private readonly InsightAlertOptions _options;
    private readonly ILogger<InsightAlertService> _logger;

    public InsightAlertService(
        IInventoryInsightsAppService inventoryInsightsAppService,
        IInsightActionAppService insightActionAppService,
        IOptions<InsightAlertOptions> options,
        ILogger<InsightAlertService> logger)
    {
        _inventoryInsightsAppService = inventoryInsightsAppService;
        _insightActionAppService = insightActionAppService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var summary = await _inventoryInsightsAppService.TryGetExecutiveSummarySnapshotAsync(
            cancellationToken: cancellationToken);

        if (summary is null)
        {
            _logger.LogInformation(
                "Skipping insight alerts: executive summary snapshot is not ready. Run the insight_snapshots job first.");
            return;
        }

        using var alertCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        alertCts.CancelAfter(TimeSpan.FromSeconds(20));
        cancellationToken = alertCts.Token;

        if (summary.DeadStockCount >= _options.DeadStockCountThreshold)
        {
            await RecordAlertAsync(
                "executive_summary",
                "dead_stock_threshold",
                $"Dead stock count {summary.DeadStockCount} exceeds threshold {_options.DeadStockCountThreshold}.",
                cancellationToken);
        }

        if (summary.TiedCapital >= _options.TiedCapitalThreshold)
        {
            await RecordAlertAsync(
                "executive_summary",
                "tied_capital_threshold",
                $"Tied capital {summary.TiedCapital:N0} exceeds threshold {_options.TiedCapitalThreshold:N0}.",
                cancellationToken);
        }

        if (summary.ReorderRiskCount >= _options.ReorderRiskCountThreshold)
        {
            await RecordAlertAsync(
                "executive_summary",
                "reorder_risk_threshold",
                $"Reorder risk count {summary.ReorderRiskCount} exceeds threshold {_options.ReorderRiskCountThreshold}.",
                cancellationToken);
        }
    }

    private async Task RecordAlertAsync(
        string insightKind,
        string actionCode,
        string message,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Insight alert: {Message}", message);

        try
        {
            await _insightActionAppService.RecordAsync(new RecordInsightActionDto
            {
                InsightKind = insightKind,
                ActionCode = actionCode,
                ActionStatus = InsightActionStatus.AlertTriggered,
                Payload = new Dictionary<string, string> { ["message"] = message }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist insight alert log for {ActionCode}.", actionCode);
        }
    }
}
