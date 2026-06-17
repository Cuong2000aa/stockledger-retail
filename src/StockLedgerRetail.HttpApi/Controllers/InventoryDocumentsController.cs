using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

[ApiController]
[Route("api/inventory-documents")]
public class InventoryDocumentsController : ControllerBase
{
    private readonly IInventoryDocumentAppService _inventoryDocumentAppService;

    public InventoryDocumentsController(IInventoryDocumentAppService inventoryDocumentAppService)
    {
        _inventoryDocumentAppService = inventoryDocumentAppService;
    }

    [HttpGet]
    public Task<List<InventoryDocumentDto>> GetListAsync(
        [FromQuery] InventoryDocumentType? documentType,
        CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.GetListAsync(documentType, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<InventoryDocumentDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.GetAsync(id, cancellationToken);

    [HttpPost("stock-in")]
    public Task<InventoryDocumentDto> CreateStockInAsync(
        [FromBody] CreateStockInDto input,
        CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.CreateStockInAsync(input, cancellationToken);

    [HttpPost("stock-out")]
    public Task<InventoryDocumentDto> CreateStockOutAsync(
        [FromBody] CreateStockOutDto input,
        CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.CreateStockOutAsync(input, cancellationToken);

    [HttpPost("{id:guid}/approve")]
    public Task<InventoryDocumentDto> ApproveAsync(Guid id, CancellationToken cancellationToken) =>
        _inventoryDocumentAppService.ApproveAsync(id, cancellationToken);
}
