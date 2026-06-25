using StockLedgerRetail.Audit;
using StockLedgerRetail.Caching;
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
    private readonly ILotStockService _lotStockService;
    private readonly IAuditContext _auditContext;
    private readonly IInventoryCacheInvalidator _inventoryCacheInvalidator;
    private readonly IUnitBarcodeStockService _unitBarcodeStockService;

    public StockLedgerService(
        ICurrentStockRepository currentStockRepository,
        IStockTransactionRepository stockTransactionRepository,
        IProductVariantRepository productVariantRepository,
        IWarehouseRepository warehouseRepository,
        IDocumentNumberGenerator documentNumberGenerator,
        IInventoryValuationService inventoryValuationService,
        ILotStockService lotStockService,
        IAuditContext auditContext,
        IInventoryCacheInvalidator inventoryCacheInvalidator,
        IUnitBarcodeStockService unitBarcodeStockService)
    {
        _currentStockRepository = currentStockRepository;
        _stockTransactionRepository = stockTransactionRepository;
        _productVariantRepository = productVariantRepository;
        _warehouseRepository = warehouseRepository;
        _documentNumberGenerator = documentNumberGenerator;
        _inventoryValuationService = inventoryValuationService;
        _lotStockService = lotStockService;
        _auditContext = auditContext;
        _inventoryCacheInvalidator = inventoryCacheInvalidator;
        _unitBarcodeStockService = unitBarcodeStockService;
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
                await ProcessTransferShipAsync(document, cancellationToken);
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

    public async Task ProcessTransferShipAsync(InventoryDocument document, CancellationToken cancellationToken = default)
    {
        if (document.SourceWarehouseId is null || document.InTransitWarehouseId is null)
        {
            throw new InvalidOperationException(
                "Source warehouse and in-transit warehouse are required for transfer ship.");
        }

        var sourceWarehouseId = document.SourceWarehouseId.Value;
        var inTransitWarehouseId = document.InTransitWarehouseId.Value;

        await EnsureWarehouseExistsAsync(sourceWarehouseId, cancellationToken);
        await EnsureWarehouseExistsAsync(inTransitWarehouseId, cancellationToken);

        var now = DateTime.UtcNow;

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
                inTransitWarehouseId,
                StockTransactionType.TransferIn,
                line.Quantity,
                cancellationToken);

            await ApplyTransferUnitBarcodesAsync(
                line,
                sourceWarehouseId,
                inTransitWarehouseId,
                now,
                cancellationToken);
        }
    }

    public async Task ProcessTransferReceiveAsync(InventoryDocument document, CancellationToken cancellationToken = default)
    {
        if (document.InTransitWarehouseId is null || document.DestinationWarehouseId is null)
        {
            throw new InvalidOperationException(
                "In-transit warehouse and destination warehouse are required for transfer receive.");
        }

        var inTransitWarehouseId = document.InTransitWarehouseId.Value;
        var destinationWarehouseId = document.DestinationWarehouseId.Value;
        var now = DateTime.UtcNow;

        await EnsureWarehouseExistsAsync(inTransitWarehouseId, cancellationToken);
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
                inTransitWarehouseId,
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

            await ApplyTransferReceiveUnitBarcodesAsync(
                line,
                inTransitWarehouseId,
                destinationWarehouseId,
                now,
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

        var lineBarcodes = BarcodeNormalization.FromLine(line);
        var counterpartWarehouseId = ResolveCounterpartWarehouse(document, warehouseId, transactionType);

        var transaction = new StockTransaction
        {
            Id = transactionId,
            TransactionNo = transactionNo,
            DocumentId = document.Id,
            DocumentLineId = line.Id,
            DocumentNo = document.DocumentNo,
            SourceSystem = document.SourceSystem,
            ReferenceNo = document.ReferenceNo,
            CounterpartWarehouseId = counterpartWarehouseId,
            ProductVariantId = line.ProductVariantId,
            WarehouseId = warehouseId,
            TransactionType = transactionType,
            QuantityDelta = quantityDelta,
            BeforeQuantity = change.BeforeOnHand,
            AfterQuantity = change.AfterOnHand,
            UnitCost = unitCost,
            TransactionDate = document.DocumentDate,
            CreatedBy = _auditContext.UserName,
            CreatedAt = now,
            Barcodes = lineBarcodes
                .Select(bc => new StockTransactionBarcode
                {
                    Id = Guid.NewGuid(),
                    StockTransactionId = transactionId,
                    Barcode = bc
                })
                .ToList()
        };

        await _stockTransactionRepository.InsertAsync(transaction, cancellationToken);

        if (quantityDelta > 0)
        {
            await _lotStockService.ApplyStockInLotAsync(line, warehouseId, now, cancellationToken);
        }
        else if (quantityDelta < 0)
        {
            await _lotStockService.ApplyStockOutFefoAsync(line, warehouseId, now, cancellationToken);
        }

        await ApplyUnitBarcodeChangeAsync(
            line,
            warehouseId,
            transactionType,
            quantityDelta,
            now,
            cancellationToken);

        await _inventoryValuationService.UpsertSnapshotAsync(
            line.ProductVariantId,
            warehouseId,
            change.AfterOnHand,
            change.QuantityReserved,
            change.QuantityAvailable,
            now,
            cancellationToken);

        await _inventoryCacheInvalidator.InvalidateStockAsync(
            warehouseId,
            line.ProductVariantId,
            cancellationToken);
    }

    private async Task ApplyUnitBarcodeChangeAsync(
        InventoryDocumentLine line,
        Guid warehouseId,
        StockTransactionType transactionType,
        decimal quantityDelta,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var variant = await _productVariantRepository.GetByIdAsync(line.ProductVariantId, cancellationToken);
        if (variant is null || !variant.IsBarcode)
        {
            return;
        }

        var barcodes = BarcodeNormalization.FromLine(line);
        if (barcodes.Count == 0)
        {
            return;
        }

        switch (transactionType)
        {
            case StockTransactionType.TransferIn:
            case StockTransactionType.TransferOut:
                return;
            case StockTransactionType.In:
            case StockTransactionType.AdjustmentIn:
            case StockTransactionType.CountAdjustmentIn:
                await _unitBarcodeStockService.ApplyInboundAsync(
                    line.ProductVariantId,
                    warehouseId,
                    barcodes,
                    now,
                    cancellationToken);
                break;
            case StockTransactionType.Out:
            case StockTransactionType.AdjustmentOut:
            case StockTransactionType.CountAdjustmentOut:
                await _unitBarcodeStockService.ApplyOutboundAsync(
                    line.ProductVariantId,
                    warehouseId,
                    barcodes,
                    now,
                    cancellationToken);
                break;
        }
    }

    private async Task ApplyTransferUnitBarcodesAsync(
        InventoryDocumentLine line,
        Guid sourceWarehouseId,
        Guid inTransitWarehouseId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var variant = await _productVariantRepository.GetByIdAsync(line.ProductVariantId, cancellationToken);
        if (variant is null || !variant.IsBarcode)
        {
            return;
        }

        var barcodes = BarcodeNormalization.FromLine(line);
        if (barcodes.Count == 0)
        {
            return;
        }

        await _unitBarcodeStockService.ApplyTransferShipAsync(
            line.ProductVariantId,
            sourceWarehouseId,
            inTransitWarehouseId,
            barcodes,
            now,
            cancellationToken);
    }

    private async Task ApplyTransferReceiveUnitBarcodesAsync(
        InventoryDocumentLine line,
        Guid inTransitWarehouseId,
        Guid destinationWarehouseId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var variant = await _productVariantRepository.GetByIdAsync(line.ProductVariantId, cancellationToken);
        if (variant is null || !variant.IsBarcode)
        {
            return;
        }

        var barcodes = BarcodeNormalization.FromLine(line);
        if (barcodes.Count == 0)
        {
            return;
        }

        await _unitBarcodeStockService.ApplyTransferReceiveAsync(
            line.ProductVariantId,
            inTransitWarehouseId,
            destinationWarehouseId,
            barcodes,
            now,
            cancellationToken);
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

    private static Guid? ResolveCounterpartWarehouse(
        InventoryDocument document,
        Guid warehouseId,
        StockTransactionType transactionType) =>
        transactionType switch
        {
            StockTransactionType.In or StockTransactionType.AdjustmentIn or StockTransactionType.CountAdjustmentIn
                => document.SourceWarehouseId,
            StockTransactionType.Out or StockTransactionType.AdjustmentOut or StockTransactionType.CountAdjustmentOut
                => document.DestinationWarehouseId,
            StockTransactionType.TransferIn
                => document.SourceWarehouseId ?? document.InTransitWarehouseId,
            StockTransactionType.TransferOut
                => document.DestinationWarehouseId ?? document.InTransitWarehouseId,
            _ => null
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
