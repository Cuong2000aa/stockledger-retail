using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Insights;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

[ApiController]
[Route("api/insight-actions")]
public class InsightActionsController : ControllerBase
{
    private readonly IInsightActionAppService _insightActionAppService;

    public InsightActionsController(IInsightActionAppService insightActionAppService)
    {
        _insightActionAppService = insightActionAppService;
    }

    [HttpPost]
    public Task<InsightActionLogDto> RecordAsync(
        [FromBody] RecordInsightActionDto input,
        CancellationToken cancellationToken = default) =>
        _insightActionAppService.RecordAsync(input, cancellationToken);

    [HttpGet("recent")]
    public Task<List<InsightActionLogDto>> GetRecentAsync(
        [FromQuery] int limit = 50,
        [FromQuery] string? insightKind = null,
        CancellationToken cancellationToken = default) =>
        _insightActionAppService.GetRecentAsync(limit, insightKind, cancellationToken);

    [HttpGet("stats")]
    public Task<InsightActionStatsDto> GetStatsAsync(
        [FromQuery] int lookbackDays = 30,
        CancellationToken cancellationToken = default) =>
        _insightActionAppService.GetStatsAsync(lookbackDays, cancellationToken);
}
