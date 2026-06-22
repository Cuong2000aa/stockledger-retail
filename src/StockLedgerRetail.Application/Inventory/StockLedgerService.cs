using StockLedgerRetail.Audit;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Application.Inventory;

/// <summary>
/// Engine sổ cái tồn kho — khi phiếu được duyệt, sinh StockTransaction và cập nhật CurrentStock.
/// Nguyên tắc: không đổi tồn mà không có giao dịch; không cho tồn âm.
/// </summary>
public class StockLedgerService : IStockLedgerService
{
    private readonly ICurrentStockRepository _currentStockRepository;
    private readonly IStockTransactionRepository _stockTransactionRepository;
    private readonly IProductVariantRepository _productVariantRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IAuditContext _auditContext;

    public StockLedgerService(
        ICurrentStockRepository currentStockRepository,
        IStockTransactionRepository stockTransactionRepository,
        IProductVariantRepository productVariantRepository,
        IWarehouseRepository warehouseRepository,
        IAuditContext auditContext)
    {
        _currentStockRepository = currentStockRepository;
        _stockTransactionRepository = stockTransactionRepository;
        _productVariantRepository = productVariantRepository;
        _warehouseRepository = warehouseRepository;
        _auditContext = auditContext;
    }

    /// <summary>Xử lý phiếu đã duyệt theo loại (StockIn, StockOut, Adjustment).</summary>
    public async Task ProcessApprovedDocumentAsync(InventoryDocument document, CancellationToken cancellationToken = default)
    {
        if (document.Lines.Count == 0)
        {
            throw new InvalidOperationException("Document must contain at least one line.");
        }

        switch (document.DocumentType)
        {
            case InventoryDocumentType.StockIn:
                await ProcessStockInAsync(document, cancellationToken);
                break;
            case InventoryDocumentType.StockOut:
                await ProcessStockOutAsync(document, cancellationToken);
                break;
            case InventoryDocumentType.Adjustment:
                await ProcessAdjustmentAsync(document, cancellationToken);
                break;
            case InventoryDocumentType.Transfer:
                await ProcessTransferAsync(document, cancellationToken);
                break;
            case InventoryDocumentType.StockCount:
                await ProcessStockCountAsync(document, cancellationToken);
                break;
            default:
                throw new InvalidOperationException(
                    $"Document type '{document.DocumentType}' is not supported yet.");
        }
    }

    /// <summary>Xử lý nhập kho — tăng tồn tại kho đích cho từng dòng.</summary>
    private async Task ProcessStockInAsync(InventoryDocument document, CancellationToken cancellationToken)
    {
        if (document.DestinationWarehouseId is null)
        {
            throw new InvalidOperationException("Destination warehouse is required for stock in.");
        }

        await EnsureWarehouseExistsAsync(document.DestinationWarehouseId.Value, cancellationToken);

        foreach (var line in document.Lines)
        {
            await EnsureProductVariantExistsAsync(line.ProductVariantId, cancellationToken);

            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException("Line quantity must be greater than zero.");
            }

            await ApplyStockChangeAsync(
                document,
                line,
                document.DestinationWarehouseId.Value,
                StockTransactionType.In,
                line.Quantity,
                cancellationToken);
        }
    }

    /// <summary>Xử lý xuất kho — kiểm tra đủ tồn rồi giảm tại kho nguồn.</summary>
    private async Task ProcessStockOutAsync(InventoryDocument document, CancellationToken cancellationToken)
    {
        if (document.SourceWarehouseId is null)
        {
            throw new InvalidOperationException("Source warehouse is required for stock out.");
        }

        await EnsureWarehouseExistsAsync(document.SourceWarehouseId.Value, cancellationToken);

        foreach (var line in document.Lines)
        {
            await EnsureProductVariantExistsAsync(line.ProductVariantId, cancellationToken);

            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException("Line quantity must be greater than zero.");
            }

            var currentStock = await _currentStockRepository.GetByVariantAndWarehouseAsync(
                line.ProductVariantId,
                document.SourceWarehouseId.Value,
                cancellationToken);

            var available = currentStock?.QuantityAvailable ?? 0;
            if (available < line.Quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient available stock for variant '{line.ProductVariantId}'. Available: {available}, requested: {line.Quantity}.");
            }

            await ApplyStockChangeAsync(
                document,
                line,
                document.SourceWarehouseId.Value,
                StockTransactionType.Out,
                -line.Quantity,
                cancellationToken);
        }
    }

    /// <summary>Xử lý điều chỉnh tồn — dương sinh ADJUSTMENT_IN, âm sinh ADJUSTMENT_OUT.</summary>
    private async Task ProcessAdjustmentAsync(InventoryDocument document, CancellationToken cancellationToken)
    {
        if (document.DestinationWarehouseId is null)
        {
            throw new InvalidOperationException("Warehouse is required for adjustment.");
        }

        var warehouseId = document.DestinationWarehouseId.Value;
        await EnsureWarehouseExistsAsync(warehouseId, cancellationToken);

        foreach (var line in document.Lines)
        {
            await EnsureProductVariantExistsAsync(line.ProductVariantId, cancellationToken);

            if (line.Quantity == 0)
            {
                throw new InvalidOperationException("Adjustment quantity cannot be zero.");
            }

            if (line.Quantity > 0)
            {
                await ApplyStockChangeAsync(
                    document,
                    line,
                    warehouseId,
                    StockTransactionType.AdjustmentIn,
                    line.Quantity,
                    cancellationToken);
            }
            else
            {
                var decreaseQuantity = Math.Abs(line.Quantity);
                var currentStock = await _currentStockRepository.GetByVariantAndWarehouseAsync(
                    line.ProductVariantId,
                    warehouseId,
                    cancellationToken);

                var available = currentStock?.QuantityAvailable ?? 0;
                if (available < decreaseQuantity)
                {
                    throw new InvalidOperationException(
                        $"Insufficient available stock for variant '{line.ProductVariantId}'. Available: {available}, requested decrease: {decreaseQuantity}.");
                }

                await ApplyStockChangeAsync(
                    document,
                    line,
                    warehouseId,
                    StockTransactionType.AdjustmentOut,
                    line.Quantity,
                    cancellationToken);
            }
        }
    }

    /// <summary>Xử lý chuyển kho — TRANSFER_OUT tại nguồn, TRANSFER_IN tại đích cho từng dòng.</summary>
    private async Task ProcessTransferAsync(InventoryDocument document, CancellationToken cancellationToken)
    {
        if (document.SourceWarehouseId is null || document.DestinationWarehouseId is null)
        {
            throw new InvalidOperationException("Source and destination warehouses are required for transfer.");
        }

        if (document.SourceWarehouseId == document.DestinationWarehouseId)
        {
            throw new InvalidOperationException("Source and destination warehouse cannot be the same.");
        }

        var sourceWarehouseId = document.SourceWarehouseId.Value;
        var destinationWarehouseId = document.DestinationWarehouseId.Value;

        await EnsureWarehouseExistsAsync(sourceWarehouseId, cancellationToken);
        await EnsureWarehouseExistsAsync(destinationWarehouseId, cancellationToken);

        foreach (var line in document.Lines)
        {
            await EnsureProductVariantExistsAsync(line.ProductVariantId, cancellationToken);

            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException("Line quantity must be greater than zero.");
            }

            var currentStock = await _currentStockRepository.GetByVariantAndWarehouseAsync(
                line.ProductVariantId,
                sourceWarehouseId,
                cancellationToken);

            var available = currentStock?.QuantityAvailable ?? 0;
            if (available < line.Quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient available stock for variant '{line.ProductVariantId}'. Available: {available}, requested: {line.Quantity}.");
            }

            await ApplyStockChangeAsync(
                document,
                line,
                sourceWarehouseId,
                StockTransactionType.TransferOut,
                -line.Quantity,
                cancellationToken);

            await ApplyStockChangeAsync(
                document,
                line,
                destinationWarehouseId,
                StockTransactionType.TransferIn,
                line.Quantity,
                cancellationToken);
        }
    }

    /// <summary>Xử lý kiểm kê — so sánh số kiểm với tồn hệ thống, sinh COUNT_ADJUSTMENT nếu có chênh lệch.</summary>
    private async Task ProcessStockCountAsync(InventoryDocument document, CancellationToken cancellationToken)
    {
        if (document.DestinationWarehouseId is null)
        {
            throw new InvalidOperationException("Warehouse is required for stock count.");
        }

        var warehouseId = document.DestinationWarehouseId.Value;
        await EnsureWarehouseExistsAsync(warehouseId, cancellationToken);

        foreach (var line in document.Lines)
        {
            await EnsureProductVariantExistsAsync(line.ProductVariantId, cancellationToken);

            if (line.Quantity < 0)
            {
                throw new InvalidOperationException("Counted quantity cannot be negative.");
            }

            var currentStock = await _currentStockRepository.GetByVariantAndWarehouseAsync(
                line.ProductVariantId,
                warehouseId,
                cancellationToken);

            var onHand = currentStock?.QuantityOnHand ?? 0;
            var variance = line.Quantity - onHand;

            if (variance == 0)
            {
                continue;
            }

            if (variance > 0)
            {
                await ApplyStockChangeAsync(
                    document,
                    line,
                    warehouseId,
                    StockTransactionType.CountAdjustmentIn,
                    variance,
                    cancellationToken);
            }
            else
            {
                var decreaseQuantity = Math.Abs(variance);
                var available = currentStock?.QuantityAvailable ?? 0;
                if (available < decreaseQuantity)
                {
                    throw new InvalidOperationException(
                        $"Insufficient available stock for variant '{line.ProductVariantId}'. Available: {available}, count decrease: {decreaseQuantity}.");
                }

                await ApplyStockChangeAsync(
                    document,
                    line,
                    warehouseId,
                    StockTransactionType.CountAdjustmentOut,
                    variance,
                    cancellationToken);
            }
        }
    }

    /// <summary>
    /// Áp dụng thay đổi tồn: ghi StockTransaction (before/after) rồi cập nhật hoặc tạo CurrentStock.
    /// </summary>
    private async Task ApplyStockChangeAsync(
        InventoryDocument document,
        InventoryDocumentLine line,
        Guid warehouseId,
        StockTransactionType transactionType,
        decimal quantityDelta,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var currentStock = await _currentStockRepository.GetByVariantAndWarehouseAsync(
            line.ProductVariantId,
            warehouseId,
            cancellationToken);

        var beforeQuantity = currentStock?.QuantityOnHand ?? 0;
        var afterQuantity = beforeQuantity + quantityDelta;

        if (afterQuantity < 0)
        {
            throw new InvalidOperationException("Inventory quantity cannot become negative.");
        }

        var transactionNo = await GenerateTransactionNoAsync(now, cancellationToken);
        var transaction = new StockTransaction
        {
            Id = Guid.NewGuid(),
            TransactionNo = transactionNo,
            DocumentId = document.Id,
            DocumentLineId = line.Id,
            ProductVariantId = line.ProductVariantId,
            WarehouseId = warehouseId,
            TransactionType = transactionType,
            QuantityDelta = quantityDelta,
            BeforeQuantity = beforeQuantity,
            AfterQuantity = afterQuantity,
            TransactionDate = document.DocumentDate,
            CreatedBy = _auditContext.UserName,
            CreatedAt = now
        };

        await _stockTransactionRepository.InsertAsync(transaction, cancellationToken);

        if (currentStock is null)
        {
            currentStock = new CurrentStock
            {
                Id = Guid.NewGuid(),
                ProductVariantId = line.ProductVariantId,
                WarehouseId = warehouseId,
                QuantityOnHand = afterQuantity,
                QuantityReserved = 0,
                QuantityAvailable = afterQuantity,
                LastTransactionId = transaction.Id,
                LastUpdatedAt = now
            };
            await _currentStockRepository.InsertAsync(currentStock, cancellationToken);
        }
        else
        {
            currentStock.QuantityOnHand = afterQuantity;
            currentStock.QuantityAvailable = afterQuantity - currentStock.QuantityReserved;
            currentStock.LastTransactionId = transaction.Id;
            currentStock.LastUpdatedAt = now;
            await _currentStockRepository.UpdateAsync(currentStock, cancellationToken);
        }
    }

    /// <summary>Sinh mã giao dịch sổ cái, ví dụ ST-20250622-000001.</summary>
    private async Task<string> GenerateTransactionNoAsync(DateTime now, CancellationToken cancellationToken)
    {
        var prefix = $"ST-{now:yyyyMMdd}-";
        var count = await _stockTransactionRepository.CountByDatePrefixAsync(prefix, cancellationToken);
        return $"{prefix}{(count + 1).ToString().PadLeft(6, '0')}";
    }

    /// <summary>Kiểm tra kho tồn tại trước khi ghi sổ.</summary>
    private async Task EnsureWarehouseExistsAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId, cancellationToken);
        if (warehouse is null)
        {
            throw new InvalidOperationException($"Warehouse '{warehouseId}' was not found.");
        }
    }

    /// <summary>Kiểm tra SKU tồn tại trước khi ghi sổ.</summary>
    private async Task EnsureProductVariantExistsAsync(Guid productVariantId, CancellationToken cancellationToken)
    {
        var variant = await _productVariantRepository.GetByIdAsync(productVariantId, cancellationToken);
        if (variant is null)
        {
            throw new InvalidOperationException($"Product variant '{productVariantId}' was not found.");
        }
    }
}
