using StockLedgerRetail.Integration;

namespace StockLedgerRetail.Services;

public interface IWarehouseFulfillmentService
{
    Task<CheckMultiWarehouseAvailabilityResponseDto> CheckAvailabilityAsync(
        CheckMultiWarehouseAvailabilityRequestDto input,
        CancellationToken cancellationToken = default);

    Task<AllocateWarehouseResponseDto> AllocateWarehouseAsync(
        AllocateWarehouseRequestDto input,
        CancellationToken cancellationToken = default);
}
