using StockLedgerRetail.Audit;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Services;

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
    private readonly IDocumentNumberGenerator _documentNumberGenerator;
    private readonly IInventoryValuationService _inventoryValuationService;
    private readonly IAuditContext _auditContext;

    public StockLedgerService(
        ICurrentStockRepository currentStockRepository,
        IStockTransactionRepository stockTransactionRepository,
        IProductVariantRepository productVariantRepository,
        IWarehouseRepository warehouseRepository,
        IDocumentNumberGenerator documentNumberGenerator,
        IInventoryValuationService inventoryValuationService,
        IAuditContext auditContext)
    {
        _currentStockRepository = currentStockRepository;
        _stockTransactionRepository = stockTransactionRepository;
        _productVariantRepository = productVariantRepository;
        _warehouseRepository = warehouseRepository;
        _documentNumberGenerator = documentNumberGenerator;
        _inventoryValuationService = inventoryValuationService;
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

            await ApplyStockChangeAsync(
                document,
                line,
                document.SourceWarehouseId.Value,
                StockTransactionType.Out,
                -line.Quantity,
                cancellationToken);
        }
    }

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

            var transactionType = line.Quantity > 0
                ? StockTransactionType.AdjustmentIn
                : StockTransactionType.AdjustmentOut;

            await ApplyStockChangeAsync(
                document,
                line,
                warehouseId,
                transactionType,
                line.Quantity,
                cancellationToken);
        }
    }

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

            await _currentStockRepository.LockVariantWarehouseAsync(
                line.ProductVariantId,
                warehouseId,
                cancellationToken);

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

            var transactionType = variance > 0
                ? StockTransactionType.CountAdjustmentIn
                : StockTransactionType.CountAdjustmentOut;

            await ApplyStockChangeAsync(
                document,
                line,
                warehouseId,
                transactionType,
                variance,
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
        var transactionId = Guid.NewGuid();

        var change = await _currentStockRepository.ApplyOnHandDeltaAsync(
            line.ProductVariantId,
            warehouseId,
            quantityDelta,
            now,
            transactionId,
            cancellationToken);

        var unitCost = await ResolveTransactionUnitCostAsync(
            document,
            line,
            transactionType,
            quantityDelta,
            change.BeforeOnHand,
            cancellationToken);

        var transactionNo = await _documentNumberGenerator.NextAsync(
            $"ST-{now:yyyyMMdd}-",
            _stockTransactionRepository.CountByDatePrefixAsync,
            6,
            cancellationToken);

        var transaction = new StockTransaction
        {
            Id = transactionId,
            TransactionNo = transactionNo,
            DocumentId = document.Id,
            DocumentLineId = line.Id,
            ProductVariantId = line.ProductVariantId,
            WarehouseId = warehouseId,
            TransactionType = transactionType,
            QuantityDelta = quantityDelta,
            BeforeQuantity = change.BeforeOnHand,
            AfterQuantity = change.AfterOnHand,
            UnitCost = unitCost,
            TransactionDate = document.DocumentDate,
            CreatedBy = _auditContext.UserName,
            CreatedAt = now
        };

        await _stockTransactionRepository.InsertAsync(transaction, cancellationToken);
    }

    private async Task<decimal?> ResolveTransactionUnitCostAsync(
        InventoryDocument document,
        InventoryDocumentLine line,
        StockTransactionType transactionType,
        decimal quantityDelta,
        decimal onHandBeforeChange,
        CancellationToken cancellationToken)
    {
        if (transactionType is StockTransactionType.TransferIn or StockTransactionType.TransferOut)
        {
            return null;
        }

        if (quantityDelta > 0)
        {
            if (line.UnitCost is decimal receiptUnitCost)
            {
                var updatedCost = await _inventoryValuationService.ApplyReceiptCostAsync(
                    line.ProductVariantId,
                    quantityDelta,
                    receiptUnitCost,
                    ResolveCostSource(document),
                    onHandBeforeChange,
                    document.DocumentDate,
                    cancellationToken);

                return updatedCost ?? receiptUnitCost;
            }

            return await _inventoryValuationService.ResolveIssueUnitCostAsync(
                line.ProductVariantId,
                cancellationToken);
        }

        return await _inventoryValuationService.ResolveIssueUnitCostAsync(
            line.ProductVariantId,
            cancellationToken);
    }

    private static CostSource ResolveCostSource(InventoryDocument document) =>
        document.SourceSystem?.ToUpperInvariant() switch
        {
            "PROCUREMENT" => CostSource.PurchaseSystem,
            "POS" or "OMS" or "ECOM" => CostSource.Pos,
            "ERP" => CostSource.Erp,
            _ => CostSource.Manual
        };

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
