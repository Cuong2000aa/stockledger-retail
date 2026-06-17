using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Inventory;

public class StockTransactionAppService : IStockTransactionAppService
{
    private readonly IStockTransactionRepository _stockTransactionRepository;

    public StockTransactionAppService(IStockTransactionRepository stockTransactionRepository)
    {
        _stockTransactionRepository = stockTransactionRepository;
    }

    public async Task<List<StockTransactionDto>> GetListAsync(
        Guid? warehouseId = null,
        Guid? productVariantId = null,
        CancellationToken cancellationToken = default)
    {
        var transactions = await _stockTransactionRepository.GetListAsync(
            warehouseId, productVariantId, cancellationToken);

        return transactions.Select(MapToDto).ToList();
    }

    private static StockTransactionDto MapToDto(Domain.Entities.StockTransaction transaction) => new()
    {
        Id = transaction.Id,
        TransactionNo = transaction.TransactionNo,
        DocumentId = transaction.DocumentId,
        ProductVariantId = transaction.ProductVariantId,
        Sku = transaction.ProductVariant.Sku,
        WarehouseId = transaction.WarehouseId,
        WarehouseCode = transaction.Warehouse.Code,
        TransactionType = transaction.TransactionType,
        QuantityDelta = transaction.QuantityDelta,
        BeforeQuantity = transaction.BeforeQuantity,
        AfterQuantity = transaction.AfterQuantity,
        TransactionDate = transaction.TransactionDate,
        CreatedBy = transaction.CreatedBy,
        CreatedAt = transaction.CreatedAt
    };
}
