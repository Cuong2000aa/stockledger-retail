using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Integration;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// Integration endpoints for external sales systems (POS, OMS, e-commerce).
/// </summary>
[ApiController]
[Route("api/integration/sales")]
public class SalesIntegrationController : ControllerBase
{
    private readonly ISalesIntegrationService _salesIntegrationService;

    public SalesIntegrationController(ISalesIntegrationService salesIntegrationService)
    {
        _salesIntegrationService = salesIntegrationService;
    }

  /// <summary>
    /// Check whether requested SKUs can be sold from a warehouse (read-only).
    /// </summary>
    [HttpPost("check-availability")]
    public Task<CheckSalesAvailabilityResponseDto> CheckAvailabilityAsync(
        [FromBody] CheckSalesAvailabilityRequestDto input,
        CancellationToken cancellationToken) =>
        _salesIntegrationService.CheckAvailabilityAsync(input, cancellationToken);

    /// <summary>
    /// Confirm a sale: creates stock-out document, approves, and updates ledger (idempotent by source + order ref).
    /// </summary>
    [HttpPost("confirm-sale")]
    public Task<ConfirmSaleResponseDto> ConfirmSaleAsync(
        [FromBody] ConfirmSaleRequestDto input,
        CancellationToken cancellationToken) =>
        _salesIntegrationService.ConfirmSaleAsync(input, cancellationToken);

    /// <summary>
    /// Confirm a return: creates stock-in document, approves, and updates ledger (idempotent by source + return ref).
    /// </summary>
    [HttpPost("confirm-return")]
    public Task<ConfirmReturnResponseDto> ConfirmReturnAsync(
        [FromBody] ConfirmReturnRequestDto input,
        CancellationToken cancellationToken) =>
        _salesIntegrationService.ConfirmReturnAsync(input, cancellationToken);
}
