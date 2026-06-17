using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.ProductVariants;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

[ApiController]
[Route("api/product-variants")]
public class ProductVariantsController : ControllerBase
{
    private readonly IProductVariantAppService _productVariantAppService;

    public ProductVariantsController(IProductVariantAppService productVariantAppService)
    {
        _productVariantAppService = productVariantAppService;
    }

    [HttpGet]
    public Task<List<ProductVariantDto>> GetListAsync(CancellationToken cancellationToken) =>
        _productVariantAppService.GetListAsync(cancellationToken);

    [HttpGet("by-product/{productId:guid}")]
    public Task<List<ProductVariantDto>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken) =>
        _productVariantAppService.GetByProductIdAsync(productId, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<ProductVariantDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _productVariantAppService.GetAsync(id, cancellationToken);

    [HttpPost]
    public Task<ProductVariantDto> CreateAsync([FromBody] CreateProductVariantDto input, CancellationToken cancellationToken) =>
        _productVariantAppService.CreateAsync(input, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<ProductVariantDto> UpdateAsync(Guid id, [FromBody] UpdateProductVariantDto input, CancellationToken cancellationToken) =>
        _productVariantAppService.UpdateAsync(id, input, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _productVariantAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
