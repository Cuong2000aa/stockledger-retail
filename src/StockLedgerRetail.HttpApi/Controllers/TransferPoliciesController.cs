using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Services;
using StockLedgerRetail.TransferPolicies;

namespace StockLedgerRetail.Controllers;

/// <summary>
/// API cấu hình chính sách chuyển kho giữa brand/kho.
/// Dùng để cho phép hoặc chặn các luồng chuyển liên quan tới đa thương hiệu.
/// </summary>
[ApiController]
[Route("api/admin/transfer-policies")]
public class TransferPoliciesController : ControllerBase
{
    private readonly ITransferPolicyAppService _transferPolicyAppService;

    public TransferPoliciesController(ITransferPolicyAppService transferPolicyAppService)
    {
        _transferPolicyAppService = transferPolicyAppService;
    }

    /// <summary>Lấy toàn bộ danh sách chính sách chuyển kho đang cấu hình.</summary>
    [HttpGet]
    public Task<List<TransferPolicyDto>> GetListAsync(CancellationToken cancellationToken) =>
        _transferPolicyAppService.GetListAsync(cancellationToken);

    /// <summary>Lấy chi tiết một chính sách chuyển kho theo Id.</summary>
    [HttpGet("{id:guid}")]
    public Task<TransferPolicyDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _transferPolicyAppService.GetAsync(id, cancellationToken);

    /// <summary>Tạo mới chính sách chuyển kho giữa kho nguồn và kho đích theo phạm vi brand.</summary>
    [HttpPost]
    public Task<TransferPolicyDto> CreateAsync(
        [FromBody] CreateTransferPolicyDto input,
        CancellationToken cancellationToken) =>
        _transferPolicyAppService.CreateAsync(input, cancellationToken);

    /// <summary>Cập nhật chính sách chuyển kho hiện có theo Id.</summary>
    [HttpPut("{id:guid}")]
    public Task<TransferPolicyDto> UpdateAsync(
        Guid id,
        [FromBody] UpdateTransferPolicyDto input,
        CancellationToken cancellationToken) =>
        _transferPolicyAppService.UpdateAsync(id, input, cancellationToken);
}
