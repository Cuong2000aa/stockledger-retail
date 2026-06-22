using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.ProductVariants;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// API quản lý biến thể sản phẩm (SKU) — đơn vị quản lý tồn kho thực tế (màu, size, season...).
/// </summary>
[ApiController]
[Route("api/product-variants")]
public class ProductVariantsController : ControllerBase
{
    private readonly IProductVariantAppService _productVariantAppService;

    public ProductVariantsController(IProductVariantAppService productVariantAppService)
    {
        _productVariantAppService = productVariantAppService;
    }

    /// <summary>Lấy danh sách tất cả SKU.</summary>
    [HttpGet]
    public Task<List<ProductVariantDto>> GetListAsync(CancellationToken cancellationToken) =>
        _productVariantAppService.GetListAsync(cancellationToken);

    /// <summary>Lấy danh sách SKU thuộc một sản phẩm cha.</summary>
    [HttpGet("by-product/{productId:guid}")]
    public Task<List<ProductVariantDto>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken) =>
        _productVariantAppService.GetByProductIdAsync(productId, cancellationToken);

    /// <summary>Lấy chi tiết một SKU theo Id.</summary>
    [HttpGet("{id:guid}")]
    public Task<ProductVariantDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _productVariantAppService.GetAsync(id, cancellationToken);

    /// <summary>Tạo SKU mới. Mã SKU phải duy nhất trong hệ thống.</summary>
    [HttpPost]
    public Task<ProductVariantDto> CreateAsync([FromBody] CreateProductVariantDto input, CancellationToken cancellationToken) =>
        _productVariantAppService.CreateAsync(input, cancellationToken);

    /// <summary>Cập nhật thông tin SKU (barcode, màu, size, season, trạng thái...).</summary>
    [HttpPut("{id:guid}")]
    public Task<ProductVariantDto> UpdateAsync(Guid id, [FromBody] UpdateProductVariantDto input, CancellationToken cancellationToken) =>
        _productVariantAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Xóa SKU theo Id.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _productVariantAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
