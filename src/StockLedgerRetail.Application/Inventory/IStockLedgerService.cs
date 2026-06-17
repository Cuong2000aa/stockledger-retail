using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Application.Inventory;

public interface IStockLedgerService
{
    Task ProcessApprovedDocumentAsync(InventoryDocument document, CancellationToken cancellationToken = default);
}
