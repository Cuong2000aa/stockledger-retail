using StockLedgerRetail.Integration;

namespace StockLedgerRetail.Services;

public interface IStockIntegrationService
{
    Task<StockDeltaResponseDto> GetStockDeltaAsync(
        StockDeltaQueryDto query,
        CancellationToken cancellationToken = default);
}
