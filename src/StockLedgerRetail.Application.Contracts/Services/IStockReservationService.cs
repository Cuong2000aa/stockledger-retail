using StockLedgerRetail.Integration;

namespace StockLedgerRetail.Services;

public interface IStockReservationService
{
    Task<ReserveStockResponseDto> ReserveAsync(
        ReserveStockRequestDto input,
        CancellationToken cancellationToken = default);

    Task<ReleaseStockReservationResponseDto> ReleaseAsync(
        ReleaseStockReservationRequestDto input,
        CancellationToken cancellationToken = default);

    Task CommitByReferencesAsync(
        string sourceSystem,
        Guid warehouseId,
        string? cartSessionId,
        string? orderReference,
        CancellationToken cancellationToken = default);

    Task RefreshExpiredReservationsAsync(CancellationToken cancellationToken = default);
}
