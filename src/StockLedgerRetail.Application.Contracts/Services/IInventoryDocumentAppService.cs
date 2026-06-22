using StockLedgerRetail.Common;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Inventory;

namespace StockLedgerRetail.Services;

public interface IInventoryDocumentAppService
{
    Task<InventoryDocumentDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResultDto<InventoryDocumentDto>> GetListAsync(
        InventoryDocumentType? documentType = null,
        InventoryDocumentStatus? status = null,
        int? page = null,
        int? pageSize = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<InventoryDocumentDto> CreateStockInAsync(CreateStockInDto input, CancellationToken cancellationToken = default);

    Task<InventoryDocumentDto> CreateStockOutAsync(CreateStockOutDto input, CancellationToken cancellationToken = default);

    Task<InventoryDocumentDto> CreateAdjustmentAsync(CreateAdjustmentDto input, CancellationToken cancellationToken = default);

    Task<InventoryDocumentDto> CreateTransferAsync(CreateTransferDto input, CancellationToken cancellationToken = default);

    Task<InventoryDocumentDto> CreateStockCountAsync(CreateStockCountDto input, CancellationToken cancellationToken = default);

    Task<InventoryDocumentDto> UpdateDraftAsync(Guid id, UpdateInventoryDocumentDraftDto input, CancellationToken cancellationToken = default);

    Task<InventoryDocumentDto> ApproveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InventoryDocumentDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
