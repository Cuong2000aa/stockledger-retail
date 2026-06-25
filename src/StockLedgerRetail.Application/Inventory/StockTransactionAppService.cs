using StockLedgerRetail.Application.Inventory;
using StockLedgerRetail.Common;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Inventory;

/// <summary>
/// Dịch vụ tra cứu lịch sử sổ cái (StockTransaction) — mọi thay đổi tồn đều có bản ghi ở đây.
/// </summary>
public class StockTransactionAppService : IStockTransactionAppService
{
    private readonly IStockTransactionRepository _stockTransactionRepository;

    public StockTransactionAppService(IStockTransactionRepository stockTransactionRepository)
    {
        _stockTransactionRepository = stockTransactionRepository;
    }

    /// <summary>Lấy danh sách giao dịch sổ cái, lọc theo kho và/hoặc SKU.</summary>
    public async Task<PagedResultDto<StockTransactionDto>> GetListAsync(
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        int? page = null,
        int? pageSize = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var (skip, take, normalizedPage, normalizedPageSize) = PagingNormalizer.Normalize(page, pageSize);
        var (transactions, totalCount) = await _stockTransactionRepository.GetPagedListAsync(
            warehouseId, productVariantId, skip, take, search, cancellationToken);
        var items = transactions.Select(MapToDto).ToList();
        return PagingNormalizer.Create(items, totalCount, normalizedPage, normalizedPageSize);
    }

    /// <summary>Chuyển entity StockTransaction sang DTO.</summary>
    private static StockTransactionDto MapToDto(Domain.Entities.StockTransaction transaction) => new()
    {
        Id = transaction.Id,
        TransactionNo = transaction.TransactionNo,
        DocumentId = transaction.DocumentId,
        DocumentNo = ResolveDocumentNo(transaction),
        SourceSystem = transaction.SourceSystem,
        ReferenceNo = transaction.ReferenceNo,
        ProductVariantId = transaction.ProductVariantId,
        Sku = transaction.ProductVariant.Sku,
        IsBarcode = transaction.ProductVariant.IsBarcode,
        WarehouseId = transaction.WarehouseId,
        WarehouseCode = transaction.Warehouse.Code,
        CounterpartWarehouseId = transaction.CounterpartWarehouseId,
        CounterpartWarehouseCode = transaction.CounterpartWarehouse?.Code,
        TransactionType = transaction.TransactionType,
        QuantityDelta = transaction.QuantityDelta,
        BeforeQuantity = transaction.BeforeQuantity,
        AfterQuantity = transaction.AfterQuantity,
        UnitCost = transaction.UnitCost,
        TransactionDate = transaction.TransactionDate,
        CreatedBy = transaction.CreatedBy,
        CreatedAt = transaction.CreatedAt,
        Barcodes = ResolveBarcodes(transaction),
    };

    private static List<string> ResolveBarcodes(Domain.Entities.StockTransaction transaction) =>
        transaction.Barcodes.Count > 0
            ? transaction.Barcodes.Select(b => b.Barcode).ToList()
            : transaction.DocumentLine is null
                ? []
                : BarcodeNormalization.FromLine(transaction.DocumentLine);

    private static string ResolveDocumentNo(Domain.Entities.StockTransaction transaction) =>
        string.IsNullOrWhiteSpace(transaction.DocumentNo)
            ? transaction.Document?.DocumentNo ?? string.Empty
            : transaction.DocumentNo;
}
