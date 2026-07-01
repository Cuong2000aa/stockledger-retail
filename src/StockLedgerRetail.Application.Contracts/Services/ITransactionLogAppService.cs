using StockLedgerRetail.Audit;
using StockLedgerRetail.Common;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Services;

public interface ITransactionLogAppService
{
    Task<PagedResultDto<TransactionLogDto>> GetListAsync(
        string? entityName = null,
        Guid? entityId = null,
        string? createdBy = null,
        AuditActionType? action = null,
        DateTime? createdFrom = null,
        DateTime? createdTo = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);
}
