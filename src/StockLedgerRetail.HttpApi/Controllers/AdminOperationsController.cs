using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Operations;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

[ApiController]
[Route("api/admin/operations")]
public class AdminOperationsController : ControllerBase
{
    private readonly IAdminOperationsAppService _adminOperationsAppService;

    public AdminOperationsController(IAdminOperationsAppService adminOperationsAppService)
    {
        _adminOperationsAppService = adminOperationsAppService;
    }

    [HttpGet]
    public Task<OperationsDashboardDto> GetDashboardAsync(CancellationToken cancellationToken) =>
        _adminOperationsAppService.GetDashboardAsync(cancellationToken);

    [HttpGet("jobs/{jobKey}/history")]
    public Task<List<BackgroundJobRunDto>> GetJobHistoryAsync(
        string jobKey,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default) =>
        _adminOperationsAppService.GetJobHistoryAsync(jobKey, limit, cancellationToken);

    [HttpPut("jobs/{jobKey}")]
    public Task<BackgroundJobDto> UpdateJobAsync(
        string jobKey,
        [FromBody] UpdateBackgroundJobDto input,
        CancellationToken cancellationToken) =>
        _adminOperationsAppService.UpdateJobAsync(jobKey, input, cancellationToken);

    [HttpPost("jobs/{jobKey}/run")]
    public Task<TriggerBackgroundJobResponseDto> TriggerJobAsync(
        string jobKey,
        CancellationToken cancellationToken) =>
        _adminOperationsAppService.TriggerJobAsync(jobKey, cancellationToken);
}
