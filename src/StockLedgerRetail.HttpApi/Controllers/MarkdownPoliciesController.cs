using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.MarkdownPolicies;
using StockLedgerRetail.Services;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// API cấu hình chính sách giảm giá (markdown) theo brand.
/// </summary>
[ApiController]
[Route("api/admin/markdown-policies")]
public class MarkdownPoliciesController : ControllerBase
{
    private readonly IMarkdownPolicyAppService _markdownPolicyAppService;

    public MarkdownPoliciesController(IMarkdownPolicyAppService markdownPolicyAppService)
    {
        _markdownPolicyAppService = markdownPolicyAppService;
    }

    /// <summary>Lấy toàn bộ chính sách markdown đang cấu hình.</summary>
    [HttpGet]
    public Task<List<MarkdownPolicyDto>> GetListAsync(CancellationToken cancellationToken) =>
        _markdownPolicyAppService.GetListAsync(cancellationToken);

    /// <summary>Lấy chi tiết một chính sách markdown theo Id.</summary>
    [HttpGet("{id:guid}")]
    public Task<MarkdownPolicyDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _markdownPolicyAppService.GetAsync(id, cancellationToken);

    /// <summary>Tạo chính sách markdown mới cho brand (tiers, margin floor, ngưỡng tồn).</summary>
    [HttpPost]
    public Task<MarkdownPolicyDto> CreateAsync(
        [FromBody] CreateMarkdownPolicyDto input,
        CancellationToken cancellationToken) =>
        _markdownPolicyAppService.CreateAsync(input, cancellationToken);

    /// <summary>Cập nhật chính sách markdown theo Id.</summary>
    [HttpPut("{id:guid}")]
    public Task<MarkdownPolicyDto> UpdateAsync(
        Guid id,
        [FromBody] UpdateMarkdownPolicyDto input,
        CancellationToken cancellationToken) =>
        _markdownPolicyAppService.UpdateAsync(id, input, cancellationToken);
}
