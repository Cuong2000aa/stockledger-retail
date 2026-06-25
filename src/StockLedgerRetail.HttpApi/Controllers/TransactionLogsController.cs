using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Audit;
using StockLedgerRetail.Common;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// API tra cứu audit log thay đổi dữ liệu theo entity và bản ghi.
/// Dùng để kiểm tra ai đã tạo/sửa/nghiệp vụ nào đã tác động lên dữ liệu.
/// </summary>
[ApiController]
[Route("api/audit-logs")]
public class TransactionLogsController : ControllerBase
{
    private readonly ITransactionLogAppService _transactionLogAppService;

    public TransactionLogsController(ITransactionLogAppService transactionLogAppService)
    {
        _transactionLogAppService = transactionLogAppService;
    }

    /// <summary>Lấy danh sách audit log có phân trang. Có thể lọc theo `entityName` và `entityId`.</summary>
    [HttpGet]
    public Task<PagedResultDto<TransactionLogDto>> GetListAsync(
        [FromQuery] string? entityName,
        [FromQuery] Guid? entityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        _transactionLogAppService.GetListAsync(entityName, entityId, page, pageSize, cancellationToken);
}
