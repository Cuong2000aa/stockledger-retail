using StockLedgerRetail.Audit;
using StockLedgerRetail.Domain.Repositories;

namespace StockLedgerRetail.HttpApi.Host.Middleware;

/// <summary>
/// Nhận diện user theo email từ header X-User-Email và load quyền từ DB.
/// Bỏ qua /api/integration và swagger.
/// </summary>
public class UserEmailAuthMiddleware
{
    public const string UserEmailHeader = "X-User-Email";

    private readonly RequestDelegate _next;
    private readonly bool _requireUserEmail;

    public UserEmailAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _requireUserEmail = configuration.GetValue("Auth:RequireUserEmail", true);
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUserContext currentUserContext,
        IAppUserRepository appUserRepository)
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
        var user = await appUserRepository.GetByEmailWithPermissionsAsync(email, context.RequestAborted);

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
            var permissionCodes = user.GroupAssignments
                .Where(x => x.Group.IsActive)
                .SelectMany(x => x.Group.Permissions)
                .Select(x => x.Permission.Code)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            mutable.SetUser(user.Id, user.Email, user.DisplayName, permissionCodes);
        }

        await _next(context);
    }

    private static bool ShouldSkip(PathString path) =>
        path.StartsWithSegments("/swagger")
        || path.StartsWithSegments("/api/integration")
        || path.StartsWithSegments("/api/auth/login");

    private static bool RequiresAuthentication(PathString path) =>
        path.StartsWithSegments("/api") && !path.StartsWithSegments("/api/integration");
}
