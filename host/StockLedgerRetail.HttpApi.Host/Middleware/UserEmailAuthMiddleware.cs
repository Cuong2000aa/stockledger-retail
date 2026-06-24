using Microsoft.Extensions.Logging;
using StockLedgerRetail.Audit;
using StockLedgerRetail.Caching;

namespace StockLedgerRetail.HttpApi.Host.Middleware;

/// <summary>
/// Nhận diện user theo email từ header X-User-Email và load quyền từ cache/DB.
/// Bỏ qua /api/integration và swagger.
/// </summary>
public class UserEmailAuthMiddleware
{
    public const string UserEmailHeader = "X-User-Email";

    private readonly RequestDelegate _next;
    private readonly bool _requireUserEmail;
    private readonly ILogger<UserEmailAuthMiddleware> _logger;

    public UserEmailAuthMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<UserEmailAuthMiddleware> logger)
    {
        _next = next;
        _requireUserEmail = configuration.GetValue("Auth:RequireUserEmail", true);
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

        if (!context.Request.Headers.TryGetValue(UserEmailHeader, out var emailHeader)
            || string.IsNullOrWhiteSpace(emailHeader))
        {
            if (_requireUserEmail && RequiresAuthentication(context.Request.Path))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = $"Missing header '{UserEmailHeader}'. User must be registered in the system."
                });
                return;
            }

            await _next(context);
            return;
        }

        var email = emailHeader.ToString().Trim().ToLowerInvariant();
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

        _logger.LogDebug(
            "Authenticated {Email} for {Method} {Path} with {PermissionCount} permission(s).",
            user.Email,
            context.Request.Method,
            context.Request.Path,
            user.PermissionCodes.Count);

        await _next(context);
    }

    private static bool ShouldSkip(PathString path) =>
        path.StartsWithSegments("/swagger")
        || path.StartsWithSegments("/api/integration")
        || path.StartsWithSegments("/api/auth/login");

    private static bool RequiresAuthentication(PathString path) =>
        path.StartsWithSegments("/api") && !path.StartsWithSegments("/api/integration");
}
