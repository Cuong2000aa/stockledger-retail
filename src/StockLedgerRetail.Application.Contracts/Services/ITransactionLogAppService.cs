using StockLedgerRetail.Audit;
using StockLedgerRetail.Common;

namespace StockLedgerRetail.Services;

public interface ITransactionLogAppService
{
    Task<PagedResultDto<TransactionLogDto>> GetListAsync(
        string? entityName = null,
        Guid? entityId = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);
}
