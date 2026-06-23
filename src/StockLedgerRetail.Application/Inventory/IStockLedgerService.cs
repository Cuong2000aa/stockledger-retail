using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Application.Inventory;

public interface IStockLedgerService
{
    Task ProcessApprovedDocumentAsync(InventoryDocument document, CancellationToken cancellationToken = default);

    Task ProcessTransferShipAsync(InventoryDocument document, CancellationToken cancellationToken = default);

    Task ProcessTransferReceiveAsync(InventoryDocument document, CancellationToken cancellationToken = default);
}
