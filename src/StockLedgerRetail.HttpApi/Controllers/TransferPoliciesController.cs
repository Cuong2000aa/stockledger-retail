using Microsoft.AspNetCore.Mvc;
using StockLedgerRetail.Services;
using StockLedgerRetail.TransferPolicies;

namespace StockLedgerRetail.Controllers;

[ApiController]
[Route("api/admin/transfer-policies")]
public class TransferPoliciesController : ControllerBase
{
    private readonly ITransferPolicyAppService _transferPolicyAppService;

    public TransferPoliciesController(ITransferPolicyAppService transferPolicyAppService)
    {
        _transferPolicyAppService = transferPolicyAppService;
    }

    [HttpGet]
    public Task<List<TransferPolicyDto>> GetListAsync(CancellationToken cancellationToken) =>
        _transferPolicyAppService.GetListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<TransferPolicyDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _transferPolicyAppService.GetAsync(id, cancellationToken);

    [HttpPost]
    public Task<TransferPolicyDto> CreateAsync(
        [FromBody] CreateTransferPolicyDto input,
        CancellationToken cancellationToken) =>
        _transferPolicyAppService.CreateAsync(input, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<TransferPolicyDto> UpdateAsync(
        Guid id,
        [FromBody] UpdateTransferPolicyDto input,
        CancellationToken cancellationToken) =>
        _transferPolicyAppService.UpdateAsync(id, input, cancellationToken);
}
