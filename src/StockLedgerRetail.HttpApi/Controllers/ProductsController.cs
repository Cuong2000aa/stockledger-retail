using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Products;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductAppService _productAppService;

    public ProductsController(IProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    [HttpGet]
    public Task<List<ProductDto>> GetListAsync(CancellationToken cancellationToken) =>
        _productAppService.GetListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<ProductDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _productAppService.GetAsync(id, cancellationToken);

    [HttpPost]
    public Task<ProductDto> CreateAsync([FromBody] CreateProductDto input, CancellationToken cancellationToken) =>
        _productAppService.CreateAsync(input, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<ProductDto> UpdateAsync(Guid id, [FromBody] UpdateProductDto input, CancellationToken cancellationToken) =>
        _productAppService.UpdateAsync(id, input, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _productAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
