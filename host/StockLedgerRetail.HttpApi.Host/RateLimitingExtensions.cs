using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using StockLedgerRetail.Identity;

namespace StockLedgerRetail.HttpApi.Host;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddStockLedgerRetailRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AuthRateLimitOptions>(configuration.GetSection(AuthRateLimitOptions.SectionName));

        var options = configuration.GetSection(AuthRateLimitOptions.SectionName).Get<AuthRateLimitOptions>()
            ?? new AuthRateLimitOptions();

        services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiterOptions.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                }

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { error = "Too many authentication attempts. Please try again later." },
                    cancellationToken);
            };

            if (!options.Enabled)
            {
                limiterOptions.AddPolicy(AuthRateLimitPolicies.Login, _ => RateLimitPartition.GetNoLimiter("login"));
                limiterOptions.AddPolicy(AuthRateLimitPolicies.Refresh, _ => RateLimitPartition.GetNoLimiter("refresh"));
                return;
            }

            limiterOptions.AddPolicy(AuthRateLimitPolicies.Login, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClientIpResolver.Resolve(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(1, options.LoginPermitLimit),
                        Window = TimeSpan.FromSeconds(Math.Max(1, options.WindowSeconds)),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            limiterOptions.AddPolicy(AuthRateLimitPolicies.Refresh, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClientIpResolver.Resolve(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(1, options.RefreshPermitLimit),
                        Window = TimeSpan.FromSeconds(Math.Max(1, options.WindowSeconds)),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });

        return services;
    }
}

internal static class ClientIpResolver
{
    public static string Resolve(HttpContext httpContext)
    {
        var forwarded = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var firstHop = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstHop))
            {
                return firstHop;
            }
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? IPAddress.Loopback.ToString();
    }
}
