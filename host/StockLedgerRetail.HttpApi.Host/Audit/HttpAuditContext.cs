using Microsoft.AspNetCore.Http;
using StockLedgerRetail.Audit;

namespace StockLedgerRetail.HttpApi.Host.Audit;

public class HttpAuditContext : IAuditContext
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpAuditContext(
        ICurrentUserContext currentUserContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _currentUserContext = currentUserContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserName => _currentUserContext.IsAuthenticated
        ? _currentUserContext.Email ?? _currentUserContext.DisplayName ?? "unknown"
        : "system";

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
