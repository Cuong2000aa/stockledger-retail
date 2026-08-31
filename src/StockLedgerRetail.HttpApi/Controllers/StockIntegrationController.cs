using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Integration;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// API tích hợp đồng bộ dữ liệu tồn kho (Delta sync, Webhook testing).
/// </summary>
[ApiController]
[Route("api/integration/stocks")]
public class StockIntegrationController : ControllerBase
{
    private readonly IStockIntegrationService _stockIntegrationService;
    private readonly IStockWebhookService _stockWebhookService;

    public StockIntegrationController(
        IStockIntegrationService stockIntegrationService,
        IStockWebhookService stockWebhookService)
    {
        _stockIntegrationService = stockIntegrationService;
        _stockWebhookService = stockWebhookService;
    }

    /// <summary>
    /// Lấy danh sách các biến động tồn kho (Delta Sync) phát sinh từ mốc thời gian sinceUtc.
    /// Dùng cho hệ thống OMS, sàn Ecom hoặc ERP quét dữ liệu thay đổi định kỳ.
    /// </summary>
    [HttpGet("delta")]
    public Task<StockDeltaResponseDto> GetStockDeltaAsync(
        [FromQuery] DateTime sinceUtc,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? brandId = null,
        [FromQuery] int limit = 500,
        CancellationToken cancellationToken = default) =>
        _stockIntegrationService.GetStockDeltaAsync(new StockDeltaQueryDto
        {
            SinceUtc = sinceUtc,
            WarehouseId = warehouseId,
            BrandId = brandId,
            Limit = limit
        }, cancellationToken);

    /// <summary>
    /// Gửi thử nghiệm một webhook event thay đổi tồn kho (stock.changed.test) tới các URL cấu hình.
    /// </summary>
    [HttpPost("webhooks/test")]
    public Task<DispatchWebhookTestResponseDto> TestWebhookDispatchAsync(
        [FromQuery] string? targetUrl = null,
        CancellationToken cancellationToken = default) =>
        _stockWebhookService.TestWebhookDispatchAsync(targetUrl, cancellationToken);
}
