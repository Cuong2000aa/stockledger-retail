using StockLedgerRetail.Audit;
using StockLedgerRetail.Common;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Audit;

public class TransactionLogAppService : ITransactionLogAppService
{
    private readonly ITransactionLogRepository _transactionLogRepository;

    public TransactionLogAppService(ITransactionLogRepository transactionLogRepository)
    {
        _transactionLogRepository = transactionLogRepository;
    }

    public async Task<PagedResultDto<TransactionLogDto>> GetListAsync(
        string? entityName = null,
        Guid? entityId = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var (skip, take, normalizedPage, normalizedPageSize) = PagingNormalizer.Normalize(page, pageSize);
        var (items, totalCount) = await _transactionLogRepository.GetPagedListAsync(
            entityName,
            entityId,
            skip,
            take,
            cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        return PagingNormalizer.Create(dtos, totalCount, normalizedPage, normalizedPageSize);
    }

    private static TransactionLogDto MapToDto(Domain.Entities.TransactionLog log) => new()
    {
        Id = log.Id,
        EntityName = log.EntityName,
        EntityId = log.EntityId,
        Action = log.Action,
        OldValue = log.OldValue,
        NewValue = log.NewValue,
        CreatedBy = log.CreatedBy,
        CreatedAt = log.CreatedAt,
        IpAddress = log.IpAddress
    };
}
