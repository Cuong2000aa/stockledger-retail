using Microsoft.Extensions.Options;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Integration;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Integration;

public class SalesIntegrationService : ISalesIntegrationService
{
    private readonly IInventoryDocumentRepository _inventoryDocumentRepository;
    private readonly ICurrentStockRepository _currentStockRepository;
    private readonly IProductVariantRepository _productVariantRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IInventoryDocumentAppService _inventoryDocumentAppService;
    private readonly SalesIntegrationOptions _options;

    public SalesIntegrationService(
        IInventoryDocumentRepository inventoryDocumentRepository,
        ICurrentStockRepository currentStockRepository,
        IProductVariantRepository productVariantRepository,
        IWarehouseRepository warehouseRepository,
        IInventoryDocumentAppService inventoryDocumentAppService,
        IOptions<SalesIntegrationOptions> options)
    {
        _inventoryDocumentRepository = inventoryDocumentRepository;
        _currentStockRepository = currentStockRepository;
        _productVariantRepository = productVariantRepository;
        _warehouseRepository = warehouseRepository;
        _inventoryDocumentAppService = inventoryDocumentAppService;
        _options = options.Value;
    }

    public async Task<CheckSalesAvailabilityResponseDto> CheckAvailabilityAsync(
        CheckSalesAvailabilityRequestDto input,
        CancellationToken cancellationToken = default)
    {
        await EnsureWarehouseExistsAsync(input.WarehouseId, cancellationToken);
        ValidateSalesLines(input.Lines);

        var lines = new List<SalesAvailabilityLineDto>();
        foreach (var line in input.Lines)
        {
            lines.Add(await BuildAvailabilityLineAsync(input.WarehouseId, line, cancellationToken));
        }

        return new CheckSalesAvailabilityResponseDto
        {
            WarehouseId = input.WarehouseId,
            CanFulfillAll = lines.All(x => x.IsAvailable),
            Lines = lines
        };
    }

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
                var approvedDoc = await _inventoryDocumentAppService.ApproveAsync(existing.Id, cancellationToken);
                return MapSaleResponse(approvedDoc, sourceSystem, orderReference, isReplay: false);
            }

            return MapSaleResponse(
                await _inventoryDocumentAppService.GetAsync(existing.Id, cancellationToken),
                sourceSystem,
                orderReference,
                isReplay: true);
        }

        ValidateSalesLines(input.Lines);
        await EnsureWarehouseExistsAsync(input.WarehouseId, cancellationToken);

        var documentLines = await MapToDocumentLinesAsync(input.Lines, cancellationToken);

        var created = await _inventoryDocumentAppService.CreateStockOutAsync(new CreateStockOutDto
        {
            SourceWarehouseId = input.WarehouseId,
            DocumentDate = input.SaleDate,
            ReferenceNo = orderReference,
            SourceSystem = sourceSystem,
            Note = BuildSalesNote("SALE", input.Note),
            Lines = documentLines
        }, cancellationToken);

        var approved = await _inventoryDocumentAppService.ApproveAsync(created.Id, cancellationToken);

        return MapSaleResponse(approved, sourceSystem, orderReference, isReplay: false);
    }

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
                var approvedDoc = await _inventoryDocumentAppService.ApproveAsync(existing.Id, cancellationToken);
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

        var created = await _inventoryDocumentAppService.CreateStockInAsync(new CreateStockInDto
        {
            DestinationWarehouseId = input.WarehouseId,
            DocumentDate = input.ReturnDate,
            ReferenceNo = returnReference,
            SourceSystem = sourceSystem,
            Note = BuildSalesNote("RETURN", input.Note),
            Lines = documentLines
        }, cancellationToken);

        var approved = await _inventoryDocumentAppService.ApproveAsync(created.Id, cancellationToken);

        return MapReturnResponse(approved, sourceSystem, returnReference, isReplay: false);
    }

    private async Task<SalesAvailabilityLineDto> BuildAvailabilityLineAsync(
        Guid warehouseId,
        SalesLineRequestDto line,
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

        var available = stock?.QuantityAvailable ?? 0;
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

    private string NormalizeSourceSystem(string? sourceSystem)
    {
        var normalized = string.IsNullOrWhiteSpace(sourceSystem)
            ? _options.DefaultSourceSystem
            : sourceSystem.Trim().ToUpperInvariant();

        if (_options.AllowedSourceSystems.Count > 0
            && !_options.AllowedSourceSystems.Any(x => x.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Source system '{normalized}' is not allowed.");
        }

        return normalized;
    }

    private static string NormalizeReference(string reference, string fieldName)
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
        bool isReplay) => new()
    {
        IsReplay = isReplay,
        InventoryDocumentId = document.Id,
        DocumentNo = document.DocumentNo,
        SourceSystem = sourceSystem,
        OrderReference = orderReference
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
