using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Brands;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// API quản lý brand trong mô hình đa thương hiệu.
/// Brand dùng để scope sản phẩm, SKU, kho và các luồng fulfillment/insights.
/// </summary>
[ApiController]
[Route("api/brands")]
public class BrandsController : ControllerBase
{
    private readonly IBrandAppService _brandAppService;

    public BrandsController(IBrandAppService brandAppService)
    {
        _brandAppService = brandAppService;
    }

    /// <summary>Lấy danh sách toàn bộ brand.</summary>
    [HttpGet]
    public Task<List<BrandDto>> GetListAsync(CancellationToken cancellationToken) =>
        _brandAppService.GetListAsync(cancellationToken);

    /// <summary>Lấy chi tiết một brand theo Id.</summary>
    [HttpGet("{id:guid}")]
    public Task<BrandDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _brandAppService.GetAsync(id, cancellationToken);

    /// <summary>Tạo brand mới. Mã brand (`Code`) phải duy nhất.</summary>
    [HttpPost]
    public Task<BrandDto> CreateAsync([FromBody] CreateBrandDto input, CancellationToken cancellationToken) =>
        _brandAppService.CreateAsync(input, cancellationToken);

    /// <summary>Cập nhật thông tin brand theo Id.</summary>
    [HttpPut("{id:guid}")]
    public Task<BrandDto> UpdateAsync(Guid id, [FromBody] UpdateBrandDto input, CancellationToken cancellationToken) =>
        _brandAppService.UpdateAsync(id, input, cancellationToken);
}
