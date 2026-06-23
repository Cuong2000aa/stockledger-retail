using Microsoft.Extensions.Options;
using StockLedgerRetail.Application.Integration;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Integration;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Integration;

/// <summary>
/// ATP đa kho và chọn kho xuất (ship-from-store / DC) cho omni-channel.
/// </summary>
public class WarehouseFulfillmentService : IWarehouseFulfillmentService
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ICurrentStockRepository _currentStockRepository;
    private readonly IStockReservationRepository _stockReservationRepository;
    private readonly IProductVariantRepository _productVariantRepository;
    private readonly SalesIntegrationOptions _options;

    public WarehouseFulfillmentService(
        IWarehouseRepository warehouseRepository,
        ICurrentStockRepository currentStockRepository,
        IStockReservationRepository stockReservationRepository,
        IProductVariantRepository productVariantRepository,
        IOptions<SalesIntegrationOptions> options)
    {
        _warehouseRepository = warehouseRepository;
        _currentStockRepository = currentStockRepository;
        _stockReservationRepository = stockReservationRepository;
        _productVariantRepository = productVariantRepository;
        _options = options.Value;
    }

    public async Task<CheckMultiWarehouseAvailabilityResponseDto> CheckAvailabilityAsync(
        CheckMultiWarehouseAvailabilityRequestDto input,
        CancellationToken cancellationToken = default)
    {
        ValidateSalesLines(input.Lines);
        var mappedLines = await MapLinesAsync(input.Lines, cancellationToken);
        var warehouses = await ResolveWarehousesAsync(
            input.WarehouseId,
            input.CandidateWarehouseIds,
            WarehouseSelectionMode.StoreFirst,
            null,
            cancellationToken);

        var summaries = await BuildWarehouseSummariesAsync(mappedLines, warehouses, cancellationToken);

        return new CheckMultiWarehouseAvailabilityResponseDto
        {
            CanFulfillAll = summaries.Any(x => x.CanFulfillAll),
            FulfillableWarehouseIds = summaries
                .Where(x => x.CanFulfillAll)
                .Select(x => x.WarehouseId)
                .ToList(),
            Warehouses = summaries
        };
    }

    public async Task<AllocateWarehouseResponseDto> AllocateWarehouseAsync(
        AllocateWarehouseRequestDto input,
        CancellationToken cancellationToken = default)
    {
        ValidateSalesLines(input.Lines);
        var mappedLines = await MapLinesAsync(input.Lines, cancellationToken);

        if (input.WarehouseId.HasValue)
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(input.WarehouseId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Warehouse '{input.WarehouseId}' was not found.");

            var summary = (await BuildWarehouseSummariesAsync(
                mappedLines,
                new List<Warehouse> { warehouse },
                cancellationToken)).Single();

            if (!summary.CanFulfillAll)
            {
                throw new InvalidOperationException(
                    $"Warehouse '{warehouse.Code}' cannot fulfill the order. " +
                    $"Bottleneck available: {summary.BottleneckAvailableQuantity}.");
            }

            return MapAllocation(warehouse, input.SelectionMode, summary);
        }

        var warehouses = await ResolveWarehousesAsync(
            null,
            input.CandidateWarehouseIds,
            input.SelectionMode,
            input.PreferredWarehouseId,
            cancellationToken);

        if (warehouses.Count == 0)
        {
            throw new InvalidOperationException("No active fulfillment warehouses are configured.");
        }

        var summaries = await BuildWarehouseSummariesAsync(mappedLines, warehouses, cancellationToken);
        var ordered = OrderSummariesForSelection(summaries, input.SelectionMode, input.PreferredWarehouseId);
        var selected = ordered.FirstOrDefault(x => x.CanFulfillAll)
            ?? throw new InvalidOperationException(
                "No warehouse can fulfill the full order. Check multi-warehouse availability first.");

        var selectedWarehouse = warehouses.First(x => x.Id == selected.WarehouseId);
        return MapAllocation(selectedWarehouse, input.SelectionMode, selected);
    }

    private async Task<List<WarehouseFulfillmentSummaryDto>> BuildWarehouseSummariesAsync(
        List<MappedSalesLine> mappedLines,
        List<Warehouse> warehouses,
        CancellationToken cancellationToken)
    {
        if (warehouses.Count == 0)
        {
            return new List<WarehouseFulfillmentSummaryDto>();
        }

        var variantIds = mappedLines.Select(x => x.ProductVariantId).Distinct().ToList();
        var warehouseIds = warehouses.Select(x => x.Id).ToList();

        var stocks = await _currentStockRepository.GetByVariantsAndWarehousesAsync(
            variantIds,
            warehouseIds,
            cancellationToken);

        var reserved = await _stockReservationRepository.GetActiveReservedQuantitiesAsync(
            variantIds,
            warehouseIds,
            cancellationToken);

        var stockMap = stocks.ToDictionary(x => (x.ProductVariantId, x.WarehouseId));

        var summaries = new List<WarehouseFulfillmentSummaryDto>();

        foreach (var warehouse in warehouses)
        {
            var lineResults = new List<WarehouseAvailabilityLineDto>();
            decimal bottleneck = decimal.MaxValue;

            foreach (var line in mappedLines)
            {
                stockMap.TryGetValue((line.ProductVariantId, warehouse.Id), out var stock);
                reserved.TryGetValue((line.ProductVariantId, warehouse.Id), out var reservedQty);

                var onHand = stock?.QuantityOnHand ?? 0;
                var available = onHand - reservedQty;
                var isAvailable = available >= line.Quantity;

                if (available < bottleneck)
                {
                    bottleneck = available;
                }

                lineResults.Add(new WarehouseAvailabilityLineDto
                {
                    WarehouseId = warehouse.Id,
                    WarehouseCode = warehouse.Code,
                    Sku = line.Sku,
                    ProductVariantId = line.ProductVariantId,
                    RequestedQuantity = line.Quantity,
                    AvailableQuantity = available,
                    IsAvailable = isAvailable
                });
            }

            if (bottleneck == decimal.MaxValue)
            {
                bottleneck = 0;
            }

            summaries.Add(new WarehouseFulfillmentSummaryDto
            {
                WarehouseId = warehouse.Id,
                WarehouseCode = warehouse.Code,
                WarehouseName = warehouse.Name,
                WarehouseType = warehouse.Type,
                CanFulfillAll = lineResults.All(x => x.IsAvailable),
                BottleneckAvailableQuantity = bottleneck,
                Lines = lineResults
            });
        }

        return summaries;
    }

    private async Task<List<Warehouse>> ResolveWarehousesAsync(
        Guid? warehouseId,
        IReadOnlyCollection<Guid>? candidateWarehouseIds,
        WarehouseSelectionMode selectionMode,
        Guid? preferredWarehouseId,
        CancellationToken cancellationToken)
    {
        var types = ParseFulfillmentTypes();

        if (warehouseId.HasValue)
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Warehouse '{warehouseId}' was not found.");

            if (warehouse.Status is not WarehouseStatus.Active)
            {
                throw new InvalidOperationException($"Warehouse '{warehouse.Code}' is not active.");
            }

            if (!types.Contains(warehouse.Type))
            {
                throw new InvalidOperationException(
                    $"Warehouse '{warehouse.Code}' type '{warehouse.Type}' is not allowed for fulfillment.");
            }

            return new List<Warehouse> { warehouse };
        }

        var warehouses = await _warehouseRepository.GetActiveFulfillmentWarehousesAsync(
            types,
            candidateWarehouseIds,
            cancellationToken);

        return OrderWarehouses(warehouses, selectionMode, preferredWarehouseId, candidateWarehouseIds);
    }

    private List<Warehouse> OrderWarehouses(
        List<Warehouse> warehouses,
        WarehouseSelectionMode selectionMode,
        Guid? preferredWarehouseId,
        IReadOnlyCollection<Guid>? candidateWarehouseIds)
    {
        if (selectionMode == WarehouseSelectionMode.CandidateOrder
            && candidateWarehouseIds is { Count: > 0 })
        {
            var rank = candidateWarehouseIds
                .Select((id, index) => (id, index))
                .ToDictionary(x => x.id, x => x.index);

            return warehouses
                .OrderBy(x => rank.GetValueOrDefault(x.Id, int.MaxValue))
                .ThenBy(x => x.Code)
                .ToList();
        }

        IEnumerable<Warehouse> ordered = warehouses;

        if (preferredWarehouseId.HasValue)
        {
            ordered = ordered
                .OrderByDescending(x => x.Id == preferredWarehouseId.Value)
                .ThenBy(x => x.Code);
        }

        ordered = selectionMode switch
        {
            WarehouseSelectionMode.HighestAvailableStock => ordered.OrderBy(x => x.Code),
            _ when _options.PreferStoreOverDc => ordered
                .OrderBy(x => x.Type == WarehouseType.Store ? 0 : x.Type == WarehouseType.Dc ? 1 : 2)
                .ThenBy(x => x.Code),
            _ => ordered.OrderBy(x => x.Code)
        };

        return ordered.ToList();
    }

    private static List<WarehouseFulfillmentSummaryDto> OrderSummariesForSelection(
        List<WarehouseFulfillmentSummaryDto> summaries,
        WarehouseSelectionMode selectionMode,
        Guid? preferredWarehouseId)
    {
        IEnumerable<WarehouseFulfillmentSummaryDto> ordered = summaries;

        if (preferredWarehouseId.HasValue)
        {
            ordered = ordered.OrderByDescending(x => x.WarehouseId == preferredWarehouseId.Value);
        }

        return selectionMode switch
        {
            WarehouseSelectionMode.HighestAvailableStock => ordered
                .OrderByDescending(x => x.CanFulfillAll)
                .ThenByDescending(x => x.BottleneckAvailableQuantity)
                .ThenBy(x => x.WarehouseCode)
                .ToList(),
            WarehouseSelectionMode.CandidateOrder => ordered.ToList(),
            _ => ordered
                .OrderByDescending(x => x.CanFulfillAll)
                .ThenBy(x => x.WarehouseType == WarehouseType.Store ? 0 : x.WarehouseType == WarehouseType.Dc ? 1 : 2)
                .ThenByDescending(x => x.BottleneckAvailableQuantity)
                .ThenBy(x => x.WarehouseCode)
                .ToList()
        };
    }

    private List<WarehouseType> ParseFulfillmentTypes()
    {
        var types = new List<WarehouseType>();

        foreach (var name in _options.FulfillmentWarehouseTypes)
        {
            if (Enum.TryParse<WarehouseType>(name, ignoreCase: true, out var parsed))
            {
                types.Add(parsed);
            }
        }

        if (types.Count == 0)
        {
            types.Add(WarehouseType.Store);
            types.Add(WarehouseType.Dc);
        }

        return types;
    }

    private async Task<List<MappedSalesLine>> MapLinesAsync(
        List<SalesLineRequestDto> lines,
        CancellationToken cancellationToken)
    {
        var result = new List<MappedSalesLine>();

        foreach (var line in lines)
        {
            var sku = NormalizeSku(line.Sku);
            var variant = await _productVariantRepository.GetBySkuAsync(sku, cancellationToken)
                ?? throw new InvalidOperationException($"SKU '{sku}' was not found.");

            result.Add(new MappedSalesLine(sku, variant.Id, line.Quantity));
        }

        return result;
    }

    private static AllocateWarehouseResponseDto MapAllocation(
        Warehouse warehouse,
        WarehouseSelectionMode selectionMode,
        WarehouseFulfillmentSummaryDto summary) => new()
    {
        SelectedWarehouseId = warehouse.Id,
        WarehouseCode = warehouse.Code,
        WarehouseName = warehouse.Name,
        SelectionMode = selectionMode,
        CanFulfillAll = summary.CanFulfillAll,
        Lines = summary.Lines.Select(x => new SalesAvailabilityLineDto
        {
            Sku = x.Sku,
            ProductVariantId = x.ProductVariantId,
            RequestedQuantity = x.RequestedQuantity,
            AvailableQuantity = x.AvailableQuantity,
            IsAvailable = x.IsAvailable
        }).ToList()
    };

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

    private static string NormalizeSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new InvalidOperationException("SKU is required.");
        }

        return sku.Trim();
    }

    private sealed record MappedSalesLine(string Sku, Guid ProductVariantId, decimal Quantity);
}
