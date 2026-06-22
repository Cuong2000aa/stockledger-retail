using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Products;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// API quản lý sản phẩm cha (Product) — thông tin tổng quát: mã, tên, brand, category.
/// </summary>
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductAppService _productAppService;

    public ProductsController(IProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    /// <summary>Lấy danh sách tất cả sản phẩm.</summary>
    [HttpGet]
    public Task<List<ProductDto>> GetListAsync(CancellationToken cancellationToken) =>
        _productAppService.GetListAsync(cancellationToken);

    /// <summary>Lấy chi tiết một sản phẩm theo Id.</summary>
    [HttpGet("{id:guid}")]
    public Task<ProductDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _productAppService.GetAsync(id, cancellationToken);

    /// <summary>Tạo sản phẩm mới. Mã sản phẩm (ProductCode) phải duy nhất.</summary>
    [HttpPost]
    public Task<ProductDto> CreateAsync([FromBody] CreateProductDto input, CancellationToken cancellationToken) =>
        _productAppService.CreateAsync(input, cancellationToken);

    /// <summary>Cập nhật thông tin sản phẩm (tên, brand, category, trạng thái).</summary>
    [HttpPut("{id:guid}")]
    public Task<ProductDto> UpdateAsync(Guid id, [FromBody] UpdateProductDto input, CancellationToken cancellationToken) =>
        _productAppService.UpdateAsync(id, input, cancellationToken);

    /// <summary>Xóa sản phẩm theo Id.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _productAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
