using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using StockLedgerRetail.Audit;
using StockLedgerRetail.Authorization;
using StockLedgerRetail.Caching;
using StockLedgerRetail.Identity;

namespace StockLedgerRetail.HttpApi.Host.Middleware;

/// <summary>
/// After JWT validation, load permissions from cache/DB into <see cref="ICurrentUserContext"/>.
/// Legacy <c>X-User-Email</c> header is optional when <see cref="AuthOptions.AllowLegacyEmailHeader"/> is true.
/// </summary>
public class UserContextMiddleware
{
    public const string UserEmailHeader = "X-User-Email";

    private readonly RequestDelegate _next;
    private readonly bool _requireUserEmail;
    private readonly bool _allowLegacyEmailHeader;
    private readonly ILogger<UserContextMiddleware> _logger;

    public UserContextMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<UserContextMiddleware> logger)
    {
        _next = next;
        _requireUserEmail = configuration.GetValue("Auth:RequireUserEmail", true);
        _allowLegacyEmailHeader = configuration.GetValue("Auth:AllowLegacyEmailHeader", false);
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUserContext currentUserContext,
        IUserAuthCacheService userAuthCacheService)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var email = ResolveEmail(context);
        if (string.IsNullOrWhiteSpace(email))
        {
            if (_requireUserEmail && RequiresAuthentication(context.Request.Path))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Authentication required. Send Authorization: Bearer <access_token>."
                });
                return;
            }

            await _next(context);
            return;
        }

        email = email.Trim().ToLowerInvariant();
        var user = await userAuthCacheService.GetByEmailAsync(email, context.RequestAborted);

        if (user is null || !user.IsActive)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = $"User '{email}' is not registered or inactive."
            });
            return;
        }

        if (currentUserContext is CurrentUserContext mutable)
        {
            mutable.SetUser(user.UserId, user.Email, user.DisplayName, user.PermissionCodes);
        }

        if (context.RequestServices.GetService(typeof(IUserWarehouseScopeContext)) is UserWarehouseScopeContext warehouseScope)
        {
            var unrestricted = user.PermissionCodes.Contains(PermissionCodes.SystemAdmin, StringComparer.OrdinalIgnoreCase)
                || user.PermissionCodes.Contains(PermissionCodes.InventoryScopeAllWarehouses, StringComparer.OrdinalIgnoreCase);

            if (unrestricted)
            {
                warehouseScope.SetUnrestricted();
            }
            else
            {
                warehouseScope.SetAssignments(user.WarehouseIds, user.PrimaryWarehouseId);
            }
        }

        _logger.LogDebug(
            "Authenticated {Email} for {Method} {Path} with {PermissionCount} permission(s).",
            user.Email,
            context.Request.Method,
            context.Request.Path,
            user.PermissionCodes.Count);

        await _next(context);
    }

    private string? ResolveEmail(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var email = context.User.FindFirstValue(ClaimTypes.Email)
                ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Email);
            if (!string.IsNullOrWhiteSpace(email))
            {
                return email;
            }
        }

        if (_allowLegacyEmailHeader
            && context.Request.Headers.TryGetValue(UserEmailHeader, out var emailHeader)
            && !string.IsNullOrWhiteSpace(emailHeader))
        {
            return emailHeader.ToString();
        }

        return null;
    }

    private static bool ShouldSkip(PathString path) =>
        path.StartsWithSegments("/swagger")
        || path.StartsWithSegments("/health")
        || path.StartsWithSegments("/api/integration")
        || path.StartsWithSegments("/api/auth/login")
        || path.StartsWithSegments("/api/auth/refresh")
        || path.StartsWithSegments("/api/auth/logout");

    private static bool RequiresAuthentication(PathString path) =>
        path.StartsWithSegments("/api") && !path.StartsWithSegments("/api/integration");
}
