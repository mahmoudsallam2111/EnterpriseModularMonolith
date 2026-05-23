using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace BuildingBlocks.Presentation.Middleware;

/// <summary>
/// Reads or generates an X-Correlation-Id header and:
///  - flows it through HttpContext.Items["CorrelationId"]
///  - pushes it onto the Serilog LogContext so every log line carries it
///  - echoes it back on the response so clients can correlate too.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;
    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
