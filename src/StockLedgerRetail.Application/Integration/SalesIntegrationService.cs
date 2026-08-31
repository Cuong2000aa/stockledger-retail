using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Integration;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Integration;

/// <summary>
/// Dịch vụ tích hợp bán hàng (POS/OMS) — kiểm tra tồn, xác nhận bán/trả hàng và batch sync.
/// Dùng SourceSystem + ReferenceNo để đảm bảo idempotent (gọi lại không trừ/cộng tồn 2 lần).
/// </summary>
public class SalesIntegrationService : ISalesIntegrationService
{
    private readonly IInventoryDocumentRepository _inventoryDocumentRepository;
    private readonly ICurrentStockRepository _currentStockRepository;
    private readonly IProductVariantRepository _productVariantRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IInventoryDocumentAppService _inventoryDocumentAppService;
    private readonly IStockReservationService _stockReservationService;
    private readonly IWarehouseFulfillmentService _warehouseFulfillmentService;
    private readonly IStockWebhookService _stockWebhookService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SalesIntegrationService> _logger;
    private readonly SalesIntegrationOptions _options;

    public SalesIntegrationService(
        IInventoryDocumentRepository inventoryDocumentRepository,
        ICurrentStockRepository currentStockRepository,
        IProductVariantRepository productVariantRepository,
        IWarehouseRepository warehouseRepository,
        IInventoryDocumentAppService inventoryDocumentAppService,
        IStockReservationService stockReservationService,
        IWarehouseFulfillmentService warehouseFulfillmentService,
        IStockWebhookService stockWebhookService,
        IUnitOfWork unitOfWork,
        ILogger<SalesIntegrationService> logger,
        IOptions<SalesIntegrationOptions> options)
    {
        _inventoryDocumentRepository = inventoryDocumentRepository;
        _currentStockRepository = currentStockRepository;
        _productVariantRepository = productVariantRepository;
        _warehouseRepository = warehouseRepository;
        _inventoryDocumentAppService = inventoryDocumentAppService;
        _stockReservationService = stockReservationService;
        _warehouseFulfillmentService = warehouseFulfillmentService;
        _stockWebhookService = stockWebhookService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>Kiểm tra tồn khả dụng theo SKU — chỉ đọc, không thay đổi tồn.</summary>
    public async Task<CheckSalesAvailabilityResponseDto> CheckAvailabilityAsync(
        CheckSalesAvailabilityRequestDto input,
        CancellationToken cancellationToken = default)
    {
        await EnsureWarehouseExistsAsync(input.WarehouseId, cancellationToken);
        ValidateSalesLines(input.Lines);

        var safetyBuffer = Math.Max(0m, input.SafetyStockBuffer > 0 ? input.SafetyStockBuffer : _options.DefaultSafetyStockBuffer);
        var lines = new List<SalesAvailabilityLineDto>();
        foreach (var line in input.Lines)
        {
            lines.Add(await BuildAvailabilityLineAsync(input.WarehouseId, line, safetyBuffer, cancellationToken));
        }

        return new CheckSalesAvailabilityResponseDto
        {
            WarehouseId = input.WarehouseId,
            CanFulfillAll = lines.All(x => x.IsAvailable),
            Lines = lines
        };
    }

    public Task<CheckMultiWarehouseAvailabilityResponseDto> CheckMultiWarehouseAvailabilityAsync(
        CheckMultiWarehouseAvailabilityRequestDto input,
        CancellationToken cancellationToken = default) =>
        _warehouseFulfillmentService.CheckAvailabilityAsync(input, cancellationToken);

    public Task<AllocateWarehouseResponseDto> AllocateWarehouseAsync(
        AllocateWarehouseRequestDto input,
        CancellationToken cancellationToken = default) =>
        _warehouseFulfillmentService.AllocateWarehouseAsync(input, cancellationToken);

    /// <summary>
    /// Xác nhận bán — tạo phiếu xuất, duyệt và trừ tồn.
    /// Nếu đã xử lý cùng sourceSystem + orderReference thì trả lại kết quả cũ (isReplay=true).
    /// </summary>
    public async Task<ConfirmSaleResponseDto> ConfirmSaleAsync(
        ConfirmSaleRequestDto input,
        CancellationToken cancellationToken = default)
    {
        var sourceSystem = NormalizeSourceSystem(input.SourceSystem);
        var orderReference = NormalizeReference(input.OrderReference, "Order reference");

        var existing = await _inventoryDocumentRepository.GetBySourceReferenceAsync(
            sourceSystem,
            orderReference,
            InventoryDocumentType.StockOut,
            cancellationToken);

        if (existing is not null)
        {
            if (existing.Status is not InventoryDocumentStatus.Approved
                and not InventoryDocumentStatus.Completed)
            {
                var approvedDoc = await _unitOfWork.ExecuteInTransactionAsync(
                    ct => _inventoryDocumentAppService.ApproveAsync(existing.Id, ct),
                    cancellationToken);
                return MapSaleResponse(
                    approvedDoc,
                    sourceSystem,
                    orderReference,
                    existing.SourceWarehouseId ?? Guid.Empty,
                    isReplay: false);
            }

            var existingDoc = await _inventoryDocumentAppService.GetAsync(existing.Id, cancellationToken);
            return MapSaleResponse(
                existingDoc,
                sourceSystem,
                orderReference,
                existing.SourceWarehouseId ?? existingDoc.SourceWarehouseId ?? Guid.Empty,
                isReplay: true);
        }

        ValidateSalesLines(input.Lines);

        var warehouseId = await ResolveFulfillmentWarehouseIdAsync(
            new AllocateWarehouseRequestDto
            {
                WarehouseId = input.WarehouseId,
                CandidateWarehouseIds = input.CandidateWarehouseIds,
                SelectionMode = input.SelectionMode,
                PreferredWarehouseId = input.PreferredWarehouseId,
                Lines = input.Lines
            },
            cancellationToken);

        var documentLines = await MapToDocumentLinesAsync(input.Lines, cancellationToken);

        var approved = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _stockReservationService.CommitByReferencesAsync(
                sourceSystem,
                warehouseId,
                input.CartSessionId,
                orderReference,
                ct);

            var created = await _inventoryDocumentAppService.CreateStockOutAsync(new CreateStockOutDto
            {
                SourceWarehouseId = warehouseId,
                DocumentDate = input.SaleDate,
                ReferenceNo = orderReference,
                SourceSystem = sourceSystem,
                Note = BuildSalesNote("SALE", input.Note),
                Lines = documentLines
            }, ct);

            return await _inventoryDocumentAppService.ApproveAsync(created.Id, ct);
        }, cancellationToken);

        // Dispatch stock changed webhook notification
        _ = DispatchStockChangeEventAsync(warehouseId, input.Lines, -1, "SALE", sourceSystem, orderReference);

        return MapSaleResponse(approved, sourceSystem, orderReference, warehouseId, isReplay: false);
    }

    /// <summary>
    /// Đồng bộ hàng loạt đơn bán hàng (Hỗ trợ POS offline đồng bộ khi có mạng lại).
    /// </summary>
    public async Task<BatchConfirmSaleResponseDto> BatchConfirmSaleAsync(
        BatchConfirmSaleRequestDto input,
        CancellationToken cancellationToken = default)
    {
        var sourceSystem = NormalizeSourceSystem(input.SourceSystem);
        var results = new List<BatchConfirmSaleItemResultDto>();
        var successCount = 0;
        var failedCount = 0;

        foreach (var sale in input.Sales)
        {
            sale.SourceSystem = sourceSystem;
            var orderRef = sale.OrderReference?.Trim() ?? string.Empty;

            try
            {
                var response = await ConfirmSaleAsync(sale, cancellationToken);
                results.Add(new BatchConfirmSaleItemResultDto
                {
                    OrderReference = orderRef,
                    Success = true,
                    Data = response
                });
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to confirm sale in batch for order {OrderReference}", orderRef);
                results.Add(new BatchConfirmSaleItemResultDto
                {
                    OrderReference = orderRef,
                    Success = false,
                    ErrorMessage = ex.Message
                });
                failedCount++;
            }
        }

        return new BatchConfirmSaleResponseDto
        {
            TotalCount = input.Sales.Count,
            SuccessCount = successCount,
            FailedCount = failedCount,
            Results = results
        };
    }

    /// <summary>
    /// Xác nhận trả hàng — tạo phiếu nhập, duyệt và cộng tồn.
    /// Idempotent theo sourceSystem + returnReference.
    /// </summary>
    public async Task<ConfirmReturnResponseDto> ConfirmReturnAsync(
        ConfirmReturnRequestDto input,
        CancellationToken cancellationToken = default)
    {
        var sourceSystem = NormalizeSourceSystem(input.SourceSystem);
        var returnReference = NormalizeReference(input.ReturnReference, "Return reference");

        var existing = await _inventoryDocumentRepository.GetBySourceReferenceAsync(
            sourceSystem,
            returnReference,
            InventoryDocumentType.StockIn,
            cancellationToken);

        if (existing is not null)
        {
            if (existing.Status is not InventoryDocumentStatus.Approved
                and not InventoryDocumentStatus.Completed)
            {
                var approvedDoc = await _unitOfWork.ExecuteInTransactionAsync(
                    ct => _inventoryDocumentAppService.ApproveAsync(existing.Id, ct),
                    cancellationToken);
                return MapReturnResponse(approvedDoc, sourceSystem, returnReference, isReplay: false);
            }

            return MapReturnResponse(
                await _inventoryDocumentAppService.GetAsync(existing.Id, cancellationToken),
                sourceSystem,
                returnReference,
                isReplay: true);
        }

        ValidateSalesLines(input.Lines);
        await EnsureWarehouseExistsAsync(input.WarehouseId, cancellationToken);

        var documentLines = await MapToDocumentLinesAsync(input.Lines, cancellationToken);

        var approved = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var created = await _inventoryDocumentAppService.CreateStockInAsync(new CreateStockInDto
            {
                DestinationWarehouseId = input.WarehouseId,
                DocumentDate = input.ReturnDate,
                ReferenceNo = returnReference,
                SourceSystem = sourceSystem,
                Note = BuildSalesNote("RETURN", input.Note),
                Lines = documentLines
            }, ct);

            return await _inventoryDocumentAppService.ApproveAsync(created.Id, ct);
        }, cancellationToken);

        // Dispatch stock changed webhook notification
        _ = DispatchStockChangeEventAsync(input.WarehouseId, input.Lines, 1, "RETURN", sourceSystem, returnReference);

        return MapReturnResponse(approved, sourceSystem, returnReference, isReplay: false);
    }

    /// <summary>Tính tồn khả dụng cho một dòng SKU trong kho.</summary>
    private async Task<SalesAvailabilityLineDto> BuildAvailabilityLineAsync(
        Guid warehouseId,
        SalesLineRequestDto line,
        decimal safetyBuffer,
        CancellationToken cancellationToken)
    {
        var sku = NormalizeSku(line.Sku);
        var variant = await _productVariantRepository.GetBySkuAsync(sku, cancellationToken);

        if (variant is null)
        {
            return new SalesAvailabilityLineDto
            {
                Sku = sku,
                RequestedQuantity = line.Quantity,
                AvailableQuantity = 0,
                IsAvailable = false,
                Message = $"SKU '{sku}' was not found."
            };
        }

        var stock = await _currentStockRepository.GetByVariantAndWarehouseAsync(
            variant.Id, warehouseId, cancellationToken);

        var rawAvailable = stock?.QuantityAvailable ?? 0;
        var available = Math.Max(0m, rawAvailable - safetyBuffer);
        var isAvailable = available >= line.Quantity;

        return new SalesAvailabilityLineDto
        {
            Sku = sku,
            ProductVariantId = variant.Id,
            RequestedQuantity = line.Quantity,
            AvailableQuantity = available,
            IsAvailable = isAvailable,
            Message = isAvailable ? null : $"Insufficient stock. Available: {available}."
        };
    }

    /// <summary>Chuyển dòng bán (SKU + số lượng) sang dòng phiếu nghiệp vụ (ProductVariantId).</summary>
    private async Task<List<CreateInventoryDocumentLineDto>> MapToDocumentLinesAsync(
        List<SalesLineRequestDto> lines,
        CancellationToken cancellationToken)
    {
        var result = new List<CreateInventoryDocumentLineDto>();

        foreach (var line in lines)
        {
            var sku = NormalizeSku(line.Sku);
            var variant = await _productVariantRepository.GetBySkuAsync(sku, cancellationToken)
                ?? throw new InvalidOperationException($"SKU '{sku}' was not found.");

            result.Add(new CreateInventoryDocumentLineDto
            {
                ProductVariantId = variant.Id,
                Quantity = line.Quantity
            });
        }

        return result;
    }

    private async Task<Guid> ResolveFulfillmentWarehouseIdAsync(
        AllocateWarehouseRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.WarehouseId.HasValue)
        {
            await EnsureWarehouseExistsAsync(request.WarehouseId.Value, cancellationToken);
            return request.WarehouseId.Value;
        }

        var allocation = await _warehouseFulfillmentService.AllocateWarehouseAsync(request, cancellationToken);
        return allocation.SelectedWarehouseId;
    }

    private async Task DispatchStockChangeEventAsync(
        Guid warehouseId,
        List<SalesLineRequestDto> lines,
        decimal directionMultiplier,
        string reason,
        string sourceSystem,
        string referenceNo)
    {
        try
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId);
            var warehouseCode = warehouse?.Code ?? warehouseId.ToString();

            var events = new List<StockChangedEventDto>();

            foreach (var line in lines)
            {
                var variant = await _productVariantRepository.GetBySkuAsync(line.Sku);
                if (variant is null) continue;

                var stock = await _currentStockRepository.GetByVariantAndWarehouseAsync(variant.Id, warehouseId);

                events.Add(new StockChangedEventDto
                {
                    EventId = Guid.NewGuid().ToString("N"),
                    EventType = "stock.changed",
                    Sku = line.Sku,
                    ProductVariantId = variant.Id,
                    WarehouseId = warehouseId,
                    WarehouseCode = warehouseCode,
                    OnHandQuantity = stock?.QuantityOnHand ?? 0,
                    ReservedQuantity = stock?.QuantityReserved ?? 0,
                    AvailableQuantity = stock?.QuantityAvailable ?? 0,
                    ChangeDelta = line.Quantity * directionMultiplier,
                    Reason = reason,
                    SourceSystem = sourceSystem,
                    ReferenceNo = referenceNo,
                    OccurredAtUtc = DateTime.UtcNow
                });
            }

            if (events.Count > 0)
            {
                await _stockWebhookService.NotifyBatchStockChangedAsync(events);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to emit stock changed webhook for {ReferenceNo}", referenceNo);
        }
    }

    private string NormalizeSourceSystem(string? sourceSystem)
    {
        return IntegrationSourceNormalizer.Normalize(sourceSystem, _options);
    }

    private static string NormalizeReference(string? reference, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new InvalidOperationException($"{fieldName} is required.");
        }

        return reference.Trim();
    }

    private static string NormalizeSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new InvalidOperationException("SKU is required.");
        }

        return sku.Trim();
    }

    private static void ValidateSalesLines(List<SalesLineRequestDto> lines)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("At least one line is required.");
        }

        foreach (var line in lines)
        {
            NormalizeSku(line.Sku);
            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException("Line quantity must be greater than zero.");
            }
        }
    }

    private async Task EnsureWarehouseExistsAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId, cancellationToken);
        if (warehouse is null)
        {
            throw new InvalidOperationException($"Warehouse '{warehouseId}' was not found.");
        }
    }

    private static string? BuildSalesNote(string operation, string? note)
    {
        var prefix = $"[{operation}]";
        return string.IsNullOrWhiteSpace(note) ? prefix : $"{prefix} {note.Trim()}";
    }

    private static ConfirmSaleResponseDto MapSaleResponse(
        InventoryDocumentDto document,
        string sourceSystem,
        string orderReference,
        Guid selectedWarehouseId,
        bool isReplay) => new()
    {
        IsReplay = isReplay,
        InventoryDocumentId = document.Id,
        DocumentNo = document.DocumentNo,
        SourceSystem = sourceSystem,
        OrderReference = orderReference,
        SelectedWarehouseId = selectedWarehouseId
    };

    private static ConfirmReturnResponseDto MapReturnResponse(
        InventoryDocumentDto document,
        string sourceSystem,
        string returnReference,
        bool isReplay) => new()
    {
        IsReplay = isReplay,
        InventoryDocumentId = document.Id,
        DocumentNo = document.DocumentNo,
        SourceSystem = sourceSystem,
        ReturnReference = returnReference
    };
}
