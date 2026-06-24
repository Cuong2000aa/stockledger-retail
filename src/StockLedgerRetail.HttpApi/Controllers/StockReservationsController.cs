using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Common;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Services;
using StockLedgerRetail.StockReservations;

namespace StockLedgerRetail.Controllers;

[ApiController]
[Route("api/stock-reservations")]
public class StockReservationsController : ControllerBase
{
    private readonly IStockReservationQueryAppService _stockReservationQueryAppService;

    public StockReservationsController(IStockReservationQueryAppService stockReservationQueryAppService)
    {
        _stockReservationQueryAppService = stockReservationQueryAppService;
    }

    [HttpGet]
    public Task<PagedResultDto<StockReservationListItemDto>> GetListAsync(
        [FromQuery] Guid? warehouseId,
        [FromQuery] StockReservationStatus? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        _stockReservationQueryAppService.GetListAsync(warehouseId, status, page, pageSize, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<StockReservationListItemDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _stockReservationQueryAppService.GetAsync(id, cancellationToken);

    [HttpPost("{id:guid}/release")]
    public Task ReleaseAsync(Guid id, CancellationToken cancellationToken) =>
        _stockReservationQueryAppService.ReleaseAsync(id, cancellationToken);
}
