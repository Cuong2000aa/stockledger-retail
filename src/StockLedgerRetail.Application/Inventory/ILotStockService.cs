using StockLedgerRetail.Domain.Entities;

namespace StockLedgerRetail.Application.Inventory;

public interface ILotStockService
{
    Task ApplyStockInLotAsync(
        InventoryDocumentLine line,
        Guid warehouseId,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task ApplyStockOutFefoAsync(
        InventoryDocumentLine line,
        Guid warehouseId,
        DateTime now,
        CancellationToken cancellationToken = default);
}
