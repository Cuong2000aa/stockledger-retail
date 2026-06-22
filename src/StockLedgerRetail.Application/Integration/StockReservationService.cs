using Microsoft.Extensions.Options;
using StockLedgerRetail.Application.Integration;
using StockLedgerRetail.Domain.Entities;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Integration;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Application.Integration;

/// <summary>
/// Giữ tồn (reserve) theo cart session hoặc order reference — không trừ OnHand, chỉ tăng QuantityReserved.
/// </summary>
public class StockReservationService : IStockReservationService
{
    private readonly IStockReservationRepository _stockReservationRepository;
    private readonly ICurrentStockRepository _currentStockRepository;
    private readonly IProductVariantRepository _productVariantRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly SalesIntegrationOptions _options;

    public StockReservationService(
        IStockReservationRepository stockReservationRepository,
        ICurrentStockRepository currentStockRepository,
        IProductVariantRepository productVariantRepository,
        IWarehouseRepository warehouseRepository,
        IOptions<SalesIntegrationOptions> options)
    {
        _stockReservationRepository = stockReservationRepository;
        _currentStockRepository = currentStockRepository;
        _productVariantRepository = productVariantRepository;
        _warehouseRepository = warehouseRepository;
        _options = options.Value;
    }

    public Task RefreshExpiredReservationsAsync(CancellationToken cancellationToken = default) =>
        ExpireStaleReservationsAsync(cancellationToken);

    public async Task<ReserveStockResponseDto> ReserveAsync(
        ReserveStockRequestDto input,
        CancellationToken cancellationToken = default)
    {
        var sourceSystem = NormalizeSourceSystem(input.SourceSystem);
        var (referenceType, referenceKey) = ResolveReference(input.CartSessionId, input.OrderReference);

        await EnsureWarehouseExistsAsync(input.WarehouseId, cancellationToken);
        ValidateSalesLines(input.Lines);
        await ExpireStaleReservationsAsync(cancellationToken);

        var mappedLines = await MapToReservationLinesAsync(input.Lines, cancellationToken);

        var existing = await _stockReservationRepository.GetActiveByReferenceAsync(
            sourceSystem,
            referenceType,
            referenceKey,
            input.WarehouseId,
            cancellationToken);

        await EnsureSufficientAvailabilityAsync(
            input.WarehouseId,
            mappedLines,
            existing,
            cancellationToken);

        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(ResolveExpiryMinutes(input.ExpiresInMinutes));

        var isUpdated = existing is not null;
        StockReservation reservation;

        if (existing is not null)
        {
            existing.Lines.Clear();
            foreach (var line in mappedLines)
            {
                line.StockReservationId = existing.Id;
                existing.Lines.Add(line);
            }

            existing.ExpiresAt = expiresAt;
            existing.UpdatedAt = now;
            reservation = existing;
            await _stockReservationRepository.UpdateAsync(reservation, cancellationToken);
        }
        else
        {
            reservation = new StockReservation
            {
                Id = Guid.NewGuid(),
                ReservationNo = await GenerateReservationNoAsync(now, cancellationToken),
                SourceSystem = sourceSystem,
                ReferenceType = referenceType,
                ReferenceKey = referenceKey,
                WarehouseId = input.WarehouseId,
                Status = StockReservationStatus.Active,
                ExpiresAt = expiresAt,
                CreatedAt = now,
                UpdatedAt = now,
                Lines = mappedLines
            };

            await _stockReservationRepository.InsertAsync(reservation, cancellationToken);
        }

        await RecalculateReservedForVariantsAsync(
            input.WarehouseId,
            mappedLines.Select(x => x.ProductVariantId),
            cancellationToken);

        await _stockReservationRepository.SaveChangesAsync(cancellationToken);

        return MapReserveResponse(reservation, input.Lines, isUpdated);
    }

    public async Task<ReleaseStockReservationResponseDto> ReleaseAsync(
        ReleaseStockReservationRequestDto input,
        CancellationToken cancellationToken = default)
    {
        var sourceSystem = NormalizeSourceSystem(input.SourceSystem);
        var (referenceType, referenceKey) = ResolveReference(input.CartSessionId, input.OrderReference);

        await EnsureWarehouseExistsAsync(input.WarehouseId, cancellationToken);
        await ExpireStaleReservationsAsync(cancellationToken);

        var reservation = await _stockReservationRepository.GetActiveByReferenceAsync(
            sourceSystem,
            referenceType,
            referenceKey,
            input.WarehouseId,
            cancellationToken);

        if (reservation is null)
        {
            return new ReleaseStockReservationResponseDto { Released = false };
        }

        var variantIds = reservation.Lines.Select(x => x.ProductVariantId).ToList();
        var now = DateTime.UtcNow;

        reservation.Status = StockReservationStatus.Released;
        reservation.ReleasedAt = now;
        reservation.UpdatedAt = now;

        await _stockReservationRepository.UpdateAsync(reservation, cancellationToken);
        await RecalculateReservedForVariantsAsync(input.WarehouseId, variantIds, cancellationToken);
        await _stockReservationRepository.SaveChangesAsync(cancellationToken);

        return new ReleaseStockReservationResponseDto
        {
            Released = true,
            StockReservationId = reservation.Id,
            ReservationNo = reservation.ReservationNo
        };
    }

    public async Task CommitByReferencesAsync(
        string sourceSystem,
        Guid warehouseId,
        string? cartSessionId,
        string? orderReference,
        CancellationToken cancellationToken = default)
    {
        await ExpireStaleReservationsAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(cartSessionId))
        {
            await CommitSingleReferenceAsync(
                sourceSystem,
                StockReservationReferenceType.CartSession,
                cartSessionId.Trim(),
                warehouseId,
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(orderReference))
        {
            var orderKey = orderReference.Trim();
            var skipOrder = !string.IsNullOrWhiteSpace(cartSessionId)
                && cartSessionId.Trim().Equals(orderKey, StringComparison.Ordinal);

            if (!skipOrder)
            {
                await CommitSingleReferenceAsync(
                    sourceSystem,
                    StockReservationReferenceType.OrderReference,
                    orderKey,
                    warehouseId,
                    cancellationToken);
            }
        }
    }

    private async Task CommitSingleReferenceAsync(
        string sourceSystem,
        StockReservationReferenceType referenceType,
        string referenceKey,
        Guid warehouseId,
        CancellationToken cancellationToken)
    {
        var reservation = await _stockReservationRepository.GetActiveByReferenceAsync(
            sourceSystem,
            referenceType,
            referenceKey,
            warehouseId,
            cancellationToken);

        if (reservation is null)
        {
            return;
        }

        var variantIds = reservation.Lines.Select(x => x.ProductVariantId).ToList();
        var now = DateTime.UtcNow;

        reservation.Status = StockReservationStatus.Committed;
        reservation.CommittedAt = now;
        reservation.UpdatedAt = now;

        await _stockReservationRepository.UpdateAsync(reservation, cancellationToken);
        await RecalculateReservedForVariantsAsync(warehouseId, variantIds, cancellationToken);
        await _stockReservationRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task ExpireStaleReservationsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expired = await _stockReservationRepository.GetExpiredActiveReservationsAsync(now, cancellationToken);

        if (expired.Count == 0)
        {
            return;
        }

        foreach (var reservation in expired)
        {
            reservation.Status = StockReservationStatus.Expired;
            reservation.UpdatedAt = now;

            await _stockReservationRepository.UpdateAsync(reservation, cancellationToken);
            await RecalculateReservedForVariantsAsync(
                reservation.WarehouseId,
                reservation.Lines.Select(x => x.ProductVariantId),
                cancellationToken);
        }

        await _stockReservationRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSufficientAvailabilityAsync(
        Guid warehouseId,
        List<StockReservationLine> newLines,
        StockReservation? existingReservation,
        CancellationToken cancellationToken)
    {
        foreach (var line in newLines)
        {
            var stock = await _currentStockRepository.GetByVariantAndWarehouseAsync(
                line.ProductVariantId,
                warehouseId,
                cancellationToken);

            var onHand = stock?.QuantityOnHand ?? 0;
            var reserved = await _stockReservationRepository.GetActiveReservedQuantityAsync(
                line.ProductVariantId,
                warehouseId,
                cancellationToken);

            if (existingReservation is not null)
            {
                var ownQty = existingReservation.Lines
                    .Where(x => x.ProductVariantId == line.ProductVariantId)
                    .Sum(x => x.Quantity);
                reserved -= ownQty;
            }

            var available = onHand - reserved;
            if (line.Quantity > available)
            {
                var sku = await ResolveSkuAsync(line.ProductVariantId, cancellationToken);
                throw new InvalidOperationException(
                    $"Insufficient available stock for SKU '{sku}'. Available: {available}, requested: {line.Quantity}.");
            }
        }
    }

    private async Task RecalculateReservedForVariantsAsync(
        Guid warehouseId,
        IEnumerable<Guid> productVariantIds,
        CancellationToken cancellationToken)
    {
        foreach (var productVariantId in productVariantIds.Distinct())
        {
            var stock = await _currentStockRepository.GetByVariantAndWarehouseAsync(
                productVariantId,
                warehouseId,
                cancellationToken);

            if (stock is null)
            {
                continue;
            }

            var reserved = await _stockReservationRepository.GetActiveReservedQuantityAsync(
                productVariantId,
                warehouseId,
                cancellationToken);

            stock.QuantityReserved = reserved;
            stock.QuantityAvailable = stock.QuantityOnHand - reserved;
            stock.LastUpdatedAt = DateTime.UtcNow;

            await _currentStockRepository.UpdateAsync(stock, cancellationToken);
        }

        await _currentStockRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<StockReservationLine>> MapToReservationLinesAsync(
        List<SalesLineRequestDto> lines,
        CancellationToken cancellationToken)
    {
        var result = new List<StockReservationLine>();

        foreach (var line in lines)
        {
            var sku = NormalizeSku(line.Sku);
            var variant = await _productVariantRepository.GetBySkuAsync(sku, cancellationToken)
                ?? throw new InvalidOperationException($"SKU '{sku}' was not found.");

            result.Add(new StockReservationLine
            {
                Id = Guid.NewGuid(),
                ProductVariantId = variant.Id,
                Quantity = line.Quantity
            });
        }

        return result;
    }

    private async Task<string> GenerateReservationNoAsync(DateTime now, CancellationToken cancellationToken)
    {
        var prefix = $"RV-{now:yyyyMMdd}-";
        var count = await _stockReservationRepository.CountByDatePrefixAsync(prefix, cancellationToken);
        return $"{prefix}{(count + 1):D4}";
    }

    private async Task<string> ResolveSkuAsync(Guid productVariantId, CancellationToken cancellationToken)
    {
        var variant = await _productVariantRepository.GetByIdAsync(productVariantId, cancellationToken);
        return variant?.Sku ?? productVariantId.ToString();
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

    private static (StockReservationReferenceType ReferenceType, string ReferenceKey) ResolveReference(
        string? cartSessionId,
        string? orderReference)
    {
        var hasCart = !string.IsNullOrWhiteSpace(cartSessionId);
        var hasOrder = !string.IsNullOrWhiteSpace(orderReference);

        if (!hasCart && !hasOrder)
        {
            throw new InvalidOperationException("Either cartSessionId or orderReference is required.");
        }

        if (hasCart)
        {
            return (StockReservationReferenceType.CartSession, cartSessionId!.Trim());
        }

        return (StockReservationReferenceType.OrderReference, orderReference!.Trim());
    }

    private int ResolveExpiryMinutes(int? expiresInMinutes)
    {
        if (expiresInMinutes.HasValue)
        {
            if (expiresInMinutes.Value <= 0)
            {
                throw new InvalidOperationException("expiresInMinutes must be greater than zero.");
            }

            return expiresInMinutes.Value;
        }

        return _options.ReservationExpiryMinutes > 0 ? _options.ReservationExpiryMinutes : 30;
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

    private static ReserveStockResponseDto MapReserveResponse(
        StockReservation reservation,
        List<SalesLineRequestDto> requestLines,
        bool isUpdated)
    {
        var skuByVariant = requestLines
            .Zip(reservation.Lines, (request, line) => new { line.ProductVariantId, request.Sku })
            .ToDictionary(x => x.ProductVariantId, x => x.Sku.Trim());

        return new ReserveStockResponseDto
        {
            StockReservationId = reservation.Id,
            ReservationNo = reservation.ReservationNo,
            SourceSystem = reservation.SourceSystem,
            ReferenceType = reservation.ReferenceType.ToString(),
            ReferenceKey = reservation.ReferenceKey,
            WarehouseId = reservation.WarehouseId,
            ExpiresAt = reservation.ExpiresAt,
            IsUpdated = isUpdated,
            Lines = reservation.Lines.Select(x => new StockReservationLineDto
            {
                Sku = skuByVariant.GetValueOrDefault(x.ProductVariantId, string.Empty),
                ProductVariantId = x.ProductVariantId,
                Quantity = x.Quantity
            }).ToList()
        };
    }
}
