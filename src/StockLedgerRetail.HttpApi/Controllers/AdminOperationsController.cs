using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Operations;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// API dashboard vận hành nền cho admin.
/// Theo dõi job nền như đối soát tồn kho, refresh insight snapshot và lịch sử chạy job.
/// </summary>
[ApiController]
[Route("api/admin/operations")]
public class AdminOperationsController : ControllerBase
{
    private readonly IAdminOperationsAppService _adminOperationsAppService;

    public AdminOperationsController(IAdminOperationsAppService adminOperationsAppService)
    {
        _adminOperationsAppService = adminOperationsAppService;
    }

    /// <summary>Lấy dashboard tổng hợp trạng thái các background job và chỉ số vận hành.</summary>
    [HttpGet]
    public Task<OperationsDashboardDto> GetDashboardAsync(CancellationToken cancellationToken) =>
        _adminOperationsAppService.GetDashboardAsync(cancellationToken);

    /// <summary>Lấy lịch sử chạy của một job theo `jobKey`, giới hạn số bản ghi bằng `limit`.</summary>
    [HttpGet("jobs/{jobKey}/history")]
    public Task<List<BackgroundJobRunDto>> GetJobHistoryAsync(
        string jobKey,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default) =>
        _adminOperationsAppService.GetJobHistoryAsync(jobKey, limit, cancellationToken);

    /// <summary>Cập nhật cấu hình job nền như bật/tắt, cron hoặc thông số liên quan.</summary>
    [HttpPut("jobs/{jobKey}")]
    public Task<BackgroundJobDto> UpdateJobAsync(
        string jobKey,
        [FromBody] UpdateBackgroundJobDto input,
        CancellationToken cancellationToken) =>
        _adminOperationsAppService.UpdateJobAsync(jobKey, input, cancellationToken);

    /// <summary>Kích hoạt chạy ngay một background job theo `jobKey`.</summary>
    [HttpPost("jobs/{jobKey}/run")]
    public Task<TriggerBackgroundJobResponseDto> TriggerJobAsync(
        string jobKey,
        CancellationToken cancellationToken) =>
        _adminOperationsAppService.TriggerJobAsync(jobKey, cancellationToken);
}
