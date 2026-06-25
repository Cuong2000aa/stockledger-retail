using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Common;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Services;
using StockLedgerRetail.StockReservations;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// API quản lý giữ tồn tạm thời cho POS/OMS.
/// Reservation giúp giữ khả dụng theo giỏ hàng/đơn hàng mà chưa trừ tồn thực tế.
/// </summary>
[ApiController]
[Route("api/stock-reservations")]
public class StockReservationsController : ControllerBase
{
    private readonly IStockReservationQueryAppService _stockReservationQueryAppService;

    public StockReservationsController(IStockReservationQueryAppService stockReservationQueryAppService)
    {
        _stockReservationQueryAppService = stockReservationQueryAppService;
    }

    /// <summary>Lấy danh sách reservation có phân trang, lọc theo kho và trạng thái giữ tồn.</summary>
    [HttpGet]
    public Task<PagedResultDto<StockReservationListItemDto>> GetListAsync(
        [FromQuery] Guid? warehouseId,
        [FromQuery] StockReservationStatus? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        _stockReservationQueryAppService.GetListAsync(warehouseId, status, page, pageSize, cancellationToken);

    /// <summary>Lấy chi tiết một reservation theo Id.</summary>
    [HttpGet("{id:guid}")]
    public Task<StockReservationListItemDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _stockReservationQueryAppService.GetAsync(id, cancellationToken);

    /// <summary>Giải phóng reservation thủ công theo Id, trả lại khả dụng cho hệ thống bán hàng.</summary>
    [HttpPost("{id:guid}/release")]
    public Task ReleaseAsync(Guid id, CancellationToken cancellationToken) =>
        _stockReservationQueryAppService.ReleaseAsync(id, cancellationToken);
}
