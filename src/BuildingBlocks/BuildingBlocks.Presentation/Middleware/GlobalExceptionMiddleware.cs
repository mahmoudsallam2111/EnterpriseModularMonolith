using BuildingBlocks.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Presentation.Middleware;

/// <summary>
/// Catches every unhandled exception, logs it with the correlation id, and writes
/// an RFC 7807 ProblemDetails response. Translates DomainException specifically so
/// expected business failures map to 4xx instead of 500.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try { await _next(context); }
        catch (BusinessRuleValidationException ex)
        {
            await WriteAsync(context, StatusCodes.Status409Conflict, "Business rule violation", ex.Message, ex.Code);
        }
        catch (NotFoundDomainException ex)
        {
            await WriteAsync(context, StatusCodes.Status404NotFound, "Not found", ex.Message, ex.Code);
        }
        catch (ConflictDomainException ex)
        {
            await WriteAsync(context, StatusCodes.Status409Conflict, "Conflict", ex.Message, ex.Code);
        }
        catch (DomainException ex)
        {
            await WriteAsync(context, StatusCodes.Status400BadRequest, "Domain error", ex.Message, ex.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "Server error",
                "An unexpected error occurred. The correlation id has been logged.", "Server.Unhandled");
        }
    }

    private static async Task WriteAsync(HttpContext context, int status, string title, string detail, string code)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://errors.emm/{code}"
        };
        problem.Extensions["code"] = code;
        if (context.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var cid))
            problem.Extensions["correlationId"] = cid;
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}
