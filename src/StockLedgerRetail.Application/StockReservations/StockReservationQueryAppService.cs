using StockLedgerRetail.Common;
using StockLedgerRetail.Domain.Repositories;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Integration;
using StockLedgerRetail.Services;
using StockLedgerRetail.StockReservations;

namespace StockLedgerRetail.Application.StockReservations;

public class StockReservationQueryAppService : IStockReservationQueryAppService
{
    private readonly IStockReservationRepository _stockReservationRepository;
    private readonly IStockReservationService _stockReservationService;
    private readonly IWarehouseScopeService _warehouseScopeService;

    public StockReservationQueryAppService(
        IStockReservationRepository stockReservationRepository,
        IStockReservationService stockReservationService,
        IWarehouseScopeService warehouseScopeService)
    {
        _stockReservationRepository = stockReservationRepository;
        _stockReservationService = stockReservationService;
        _warehouseScopeService = warehouseScopeService;
    }

    public async Task<PagedResultDto<StockReservationListItemDto>> GetListAsync(
        Guid? warehouseId = null,
        StockReservationStatus? status = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var scope = _warehouseScopeService.ResolveListScope(warehouseId);
        var (skip, take, normalizedPage, normalizedPageSize) = PagingNormalizer.Normalize(page, pageSize);
        var (items, totalCount) = await _stockReservationRepository.GetPagedListAsync(
            scope.WarehouseId,
            status,
            skip,
            take,
            scope.ScopedWarehouseIds,
            cancellationToken);

        return PagingNormalizer.Create(
            items.Select(MapToDto).ToList(),
            totalCount,
            normalizedPage,
            normalizedPageSize);
    }

    public async Task<StockReservationListItemDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var reservation = await _stockReservationRepository.GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Stock reservation '{id}' was not found.");

        _warehouseScopeService.EnsureWarehouseAccess(reservation.WarehouseId);

        return MapToDto(reservation);
    }

    public async Task ReleaseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var reservation = await _stockReservationRepository.GetByIdWithLinesAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Stock reservation '{id}' was not found.");

        _warehouseScopeService.EnsureWarehouseAccess(reservation.WarehouseId);

        if (reservation.Status is not StockReservationStatus.Active)
        {
            throw new InvalidOperationException("Only active reservations can be released.");
        }

        await _stockReservationService.ReleaseAsync(new ReleaseStockReservationRequestDto
        {
            SourceSystem = reservation.SourceSystem,
            WarehouseId = reservation.WarehouseId,
            CartSessionId = reservation.ReferenceType == StockReservationReferenceType.CartSession
                ? reservation.ReferenceKey
                : null,
            OrderReference = reservation.ReferenceType == StockReservationReferenceType.OrderReference
                ? reservation.ReferenceKey
                : null
        }, cancellationToken);
    }

    private static StockReservationListItemDto MapToDto(Domain.Entities.StockReservation reservation) => new()
    {
        Id = reservation.Id,
        ReservationNo = reservation.ReservationNo,
        SourceSystem = reservation.SourceSystem,
        ReferenceType = reservation.ReferenceType,
        ReferenceKey = reservation.ReferenceKey,
        WarehouseId = reservation.WarehouseId,
        WarehouseCode = reservation.Warehouse?.Code ?? string.Empty,
        Status = reservation.Status,
        ExpiresAt = reservation.ExpiresAt,
        CreatedAt = reservation.CreatedAt,
        TotalQuantity = reservation.Lines.Sum(x => x.Quantity),
        Lines = reservation.Lines.Select(x => new StockReservationListLineDto
        {
            ProductVariantId = x.ProductVariantId,
            Sku = x.ProductVariant?.Sku ?? string.Empty,
            Quantity = x.Quantity
        }).ToList()
    };
}
