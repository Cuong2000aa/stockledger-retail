using StockLedgerRetail.Audit;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Application.Inventory;

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
            default:
                throw new InvalidOperationException(
                    $"Document type '{document.DocumentType}' is not supported yet.");
        }
    }

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

    private async Task<string> GenerateTransactionNoAsync(DateTime now, CancellationToken cancellationToken)
    {
        var prefix = $"ST-{now:yyyyMMdd}-";
        var count = await _stockTransactionRepository.CountByDatePrefixAsync(prefix, cancellationToken);
        return $"{prefix}{(count + 1).ToString().PadLeft(6, '0')}";
    }

    private async Task EnsureWarehouseExistsAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId, cancellationToken);
        if (warehouse is null)
        {
            throw new InvalidOperationException($"Warehouse '{warehouseId}' was not found.");
        }
    }

    private async Task EnsureProductVariantExistsAsync(Guid productVariantId, CancellationToken cancellationToken)
    {
        var variant = await _productVariantRepository.GetByIdAsync(productVariantId, cancellationToken);
        if (variant is null)
        {
            throw new InvalidOperationException($"Product variant '{productVariantId}' was not found.");
        }
    }
}
