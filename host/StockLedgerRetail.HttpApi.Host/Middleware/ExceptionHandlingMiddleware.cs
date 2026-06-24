using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace StockLedgerRetail.HttpApi.Host.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var root = Unwrap(exception);

        var statusCode = root switch
        {
            KeyNotFoundException => HttpStatusCode.NotFound,
            UnauthorizedAccessException => HttpStatusCode.Forbidden,
            InvalidOperationException => HttpStatusCode.BadRequest,
            ArgumentException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(new
        {
            error = root.Message
        });

        return context.Response.WriteAsync(payload);
    }

    private static Exception Unwrap(Exception exception)
    {
        while (exception.InnerException is { } inner
               && exception is DbUpdateException or InvalidOperationException)
        {
            exception = inner;
        }

        return exception;
    }
}
