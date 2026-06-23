using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Brands;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

[ApiController]
[Route("api/brands")]
public class BrandsController : ControllerBase
{
    private readonly IBrandAppService _brandAppService;

    public BrandsController(IBrandAppService brandAppService)
    {
        _brandAppService = brandAppService;
    }

    [HttpGet]
    public Task<List<BrandDto>> GetListAsync(CancellationToken cancellationToken) =>
        _brandAppService.GetListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<BrandDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _brandAppService.GetAsync(id, cancellationToken);

    [HttpPost]
    public Task<BrandDto> CreateAsync([FromBody] CreateBrandDto input, CancellationToken cancellationToken) =>
        _brandAppService.CreateAsync(input, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<BrandDto> UpdateAsync(Guid id, [FromBody] UpdateBrandDto input, CancellationToken cancellationToken) =>
        _brandAppService.UpdateAsync(id, input, cancellationToken);
}
