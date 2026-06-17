namespace StockLedgerRetail.HttpApi.Host.Middleware;

/// <summary>
/// Optional API key gate for external integration callers.
/// Configure Integration:Sales:ApiKey in appsettings; send header X-Integration-Api-Key.
/// </summary>
public class IntegrationApiKeyMiddleware
{
    private const string ApiKeyHeaderName = "X-Integration-Api-Key";
    private readonly RequestDelegate _next;
    private readonly string? _apiKey;

    public IntegrationApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _apiKey = configuration["Integration:Sales:ApiKey"];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)
            || !context.Request.Path.StartsWithSegments("/api/integration"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedKey)
            || providedKey != _apiKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or missing integration API key." });
            return;
        }

        await _next(context);
    }
}
