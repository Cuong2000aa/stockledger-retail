using StockLedgerRetail.Integration;

namespace StockLedgerRetail.Services;

public interface IStockWebhookService
{
    Task NotifyStockChangedAsync(
        StockChangedEventDto eventDto,
        CancellationToken cancellationToken = default);

    Task NotifyBatchStockChangedAsync(
        IEnumerable<StockChangedEventDto> eventDtos,
        CancellationToken cancellationToken = default);

    Task<DispatchWebhookTestResponseDto> TestWebhookDispatchAsync(
        string? targetUrl = null,
        CancellationToken cancellationToken = default);
}
