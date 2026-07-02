using System.Text.Json;
using StockLedgerRetail.Audit;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Insights;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Insights;

public class InsightActionAppService : IInsightActionAppService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IInsightActionLogRepository _insightActionLogRepository;
    private readonly IAuditContext _auditContext;

    public InsightActionAppService(
        IInsightActionLogRepository insightActionLogRepository,
        IAuditContext auditContext)
    {
        _insightActionLogRepository = insightActionLogRepository;
        _auditContext = auditContext;
    }

    public async Task<InsightActionLogDto> RecordAsync(
        RecordInsightActionDto input,
        CancellationToken cancellationToken = default)
    {
        var log = new InsightActionLog
        {
            Id = Guid.NewGuid(),
            InsightKind = input.InsightKind.Trim(),
            ActionCode = input.ActionCode.Trim(),
            ActionStatus = input.ActionStatus,
            ProductVariantId = input.ProductVariantId,
            WarehouseId = input.WarehouseId,
            SourceWarehouseId = input.SourceWarehouseId,
            DestinationWarehouseId = input.DestinationWarehouseId,
            PayloadJson = input.Payload is null ? null : JsonSerializer.Serialize(input.Payload, JsonOptions),
            ResultEntityId = input.ResultEntityId,
            ResultEntityType = input.ResultEntityType,
            CreatedBy = _auditContext.UserName,
            CreatedAt = DateTime.UtcNow,
            IpAddress = _auditContext.IpAddress
        };

        await _insightActionLogRepository.InsertAsync(log, cancellationToken);
        await _insightActionLogRepository.SaveChangesAsync(cancellationToken);

        return Map(log);
    }

    public async Task<List<InsightActionLogDto>> GetRecentAsync(
        int limit = 50,
        string? insightKind = null,
        CancellationToken cancellationToken = default)
    {
        var logs = await _insightActionLogRepository.GetRecentAsync(
            Math.Clamp(limit, 1, 200),
            insightKind,
            cancellationToken);

        return logs.Select(Map).ToList();
    }

    public async Task<InsightActionStatsDto> GetStatsAsync(
        int lookbackDays = 30,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = DateTime.UtcNow.AddDays(-Math.Max(1, lookbackDays));
        var stats = await _insightActionLogRepository.GetStatsAsync(fromUtc, DateTime.UtcNow, cancellationToken);

        return new InsightActionStatsDto
        {
            ViewedCount = stats.ViewedCount,
            AcceptedCount = stats.AcceptedCount,
            DismissedCount = stats.DismissedCount,
            ExecutedCount = stats.ExecutedCount,
            AlertTriggeredCount = stats.AlertTriggeredCount,
            TotalCount = stats.TotalCount,
            ExecutionRatePercent = stats.ExecutionRatePercent
        };
    }

    private static InsightActionLogDto Map(InsightActionLog log) => new()
    {
        Id = log.Id,
        InsightKind = log.InsightKind,
        ActionCode = log.ActionCode,
        ActionStatus = log.ActionStatus,
        ProductVariantId = log.ProductVariantId,
        WarehouseId = log.WarehouseId,
        CreatedBy = log.CreatedBy,
        CreatedAt = log.CreatedAt,
        ResultEntityId = log.ResultEntityId,
        ResultEntityType = log.ResultEntityType
    };
}
