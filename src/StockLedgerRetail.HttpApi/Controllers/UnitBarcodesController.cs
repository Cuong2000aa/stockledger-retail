using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Common;
using StockLedgerRetail.Enums;
using StockLedgerRetail.Inventory;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>API tra cứu barcode từng đơn vị theo SKU và kho.</summary>
[ApiController]
[Route("api/unit-barcodes")]
public class UnitBarcodesController : ControllerBase
{
    private readonly IVariantUnitBarcodeAppService _variantUnitBarcodeAppService;

    public UnitBarcodesController(IVariantUnitBarcodeAppService variantUnitBarcodeAppService)
    {
        _variantUnitBarcodeAppService = variantUnitBarcodeAppService;
    }

    /// <summary>Lấy danh sách unit barcode có phân trang theo SKU, kho, trạng thái và từ khóa.</summary>
    [HttpGet]
    public Task<PagedResultDto<VariantUnitBarcodeDto>> GetListAsync(
        [FromQuery] Guid productVariantId,
        [FromQuery] Guid warehouseId,
        [FromQuery] UnitBarcodeStatus? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        _variantUnitBarcodeAppService.GetListAsync(
            productVariantId,
            warehouseId,
            status,
            page,
            pageSize,
            search,
            cancellationToken);
}
