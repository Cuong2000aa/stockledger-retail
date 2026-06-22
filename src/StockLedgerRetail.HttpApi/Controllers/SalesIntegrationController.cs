using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Integration;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// API tích hợp hệ thống bán hàng (POS, OMS, e-commerce).
/// POS gọi các endpoint này thay vì tự quản lý tồn kho.
/// </summary>
[ApiController]
[Route("api/integration/sales")]
public class SalesIntegrationController : ControllerBase
{
    private readonly ISalesIntegrationService _salesIntegrationService;
    private readonly IStockReservationService _stockReservationService;

    public SalesIntegrationController(
        ISalesIntegrationService salesIntegrationService,
        IStockReservationService stockReservationService)
    {
        _salesIntegrationService = salesIntegrationService;
        _stockReservationService = stockReservationService;
    }

    /// <summary>
    /// Kiểm tra tồn khả dụng trước khi bán — chỉ đọc, không thay đổi tồn.
    /// POS gọi khi khách chọn hàng hoặc trước khi thanh toán.
    /// </summary>
    [HttpPost("check-availability")]
    public Task<CheckSalesAvailabilityResponseDto> CheckAvailabilityAsync(
        [FromBody] CheckSalesAvailabilityRequestDto input,
        CancellationToken cancellationToken) =>
        _salesIntegrationService.CheckAvailabilityAsync(input, cancellationToken);

    /// <summary>
    /// Giữ tồn theo cart session hoặc order reference — không trừ OnHand.
    /// Gọi lại cùng reference sẽ cập nhật dòng và gia hạn TTL.
    /// </summary>
    [HttpPost("reserve")]
    public Task<ReserveStockResponseDto> ReserveAsync(
        [FromBody] ReserveStockRequestDto input,
        CancellationToken cancellationToken) =>
        _stockReservationService.ReserveAsync(input, cancellationToken);

    /// <summary>
    /// Hủy giữ tồn khi khách bỏ giỏ hoặc hủy đơn nháp.
    /// </summary>
    [HttpPost("release-reservation")]
    public Task<ReleaseStockReservationResponseDto> ReleaseReservationAsync(
        [FromBody] ReleaseStockReservationRequestDto input,
        CancellationToken cancellationToken) =>
        _stockReservationService.ReleaseAsync(input, cancellationToken);

    /// <summary>
    /// Xác nhận bán hàng — tạo phiếu xuất, duyệt và trừ tồn trong một lần gọi.
    /// Idempotent: gọi lại cùng sourceSystem + orderReference không trừ tồn lần 2.
    /// </summary>
    [HttpPost("confirm-sale")]
    public Task<ConfirmSaleResponseDto> ConfirmSaleAsync(
        [FromBody] ConfirmSaleRequestDto input,
        CancellationToken cancellationToken) =>
        _salesIntegrationService.ConfirmSaleAsync(input, cancellationToken);

    /// <summary>
    /// Xác nhận trả hàng — tạo phiếu nhập, duyệt và cộng tồn trong một lần gọi.
    /// Idempotent: gọi lại cùng sourceSystem + returnReference không cộng tồn lần 2.
    /// </summary>
    [HttpPost("confirm-return")]
    public Task<ConfirmReturnResponseDto> ConfirmReturnAsync(
        [FromBody] ConfirmReturnRequestDto input,
        CancellationToken cancellationToken) =>
        _salesIntegrationService.ConfirmReturnAsync(input, cancellationToken);
}
