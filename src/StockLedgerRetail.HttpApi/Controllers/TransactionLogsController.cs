using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Audit;
using StockLedgerRetail.Common;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

[ApiController]
[Route("api/audit-logs")]
public class TransactionLogsController : ControllerBase
{
    private readonly ITransactionLogAppService _transactionLogAppService;

    public TransactionLogsController(ITransactionLogAppService transactionLogAppService)
    {
        _transactionLogAppService = transactionLogAppService;
    }

    [HttpGet]
    public Task<PagedResultDto<TransactionLogDto>> GetListAsync(
        [FromQuery] string? entityName,
        [FromQuery] Guid? entityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        _transactionLogAppService.GetListAsync(entityName, entityId, page, pageSize, cancellationToken);
}
