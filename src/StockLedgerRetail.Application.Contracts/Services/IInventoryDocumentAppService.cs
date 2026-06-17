using StockLedgerRetail.Enums;
using StockLedgerRetail.Inventory;

namespace StockLedgerRetail.Services;

public interface IInventoryDocumentAppService
{
    Task<InventoryDocumentDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<InventoryDocumentDto>> GetListAsync(
        InventoryDocumentType? documentType = null,
        CancellationToken cancellationToken = default);

    Task<InventoryDocumentDto> CreateStockInAsync(CreateStockInDto input, CancellationToken cancellationToken = default);

    Task<InventoryDocumentDto> CreateStockOutAsync(CreateStockOutDto input, CancellationToken cancellationToken = default);

    Task<InventoryDocumentDto> ApproveAsync(Guid id, CancellationToken cancellationToken = default);
}
