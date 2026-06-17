using StockLedgerRetail.Integration;

namespace StockLedgerRetail.Services;

public interface ISalesIntegrationService
{
    Task<CheckSalesAvailabilityResponseDto> CheckAvailabilityAsync(
        CheckSalesAvailabilityRequestDto input,
        CancellationToken cancellationToken = default);

    Task<ConfirmSaleResponseDto> ConfirmSaleAsync(
        ConfirmSaleRequestDto input,
        CancellationToken cancellationToken = default);

    Task<ConfirmReturnResponseDto> ConfirmReturnAsync(
        ConfirmReturnRequestDto input,
        CancellationToken cancellationToken = default);
}
