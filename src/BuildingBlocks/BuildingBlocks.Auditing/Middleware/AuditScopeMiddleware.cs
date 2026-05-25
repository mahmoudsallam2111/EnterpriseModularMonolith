using System.Diagnostics;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Auditing.Abstractions;
using BuildingBlocks.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Auditing.Middleware;

/// <summary>
/// Wraps every HTTP request in an outer audit scope. Endpoints that don't go
/// through MediatR (file uploads, raw endpoints, health checks) still produce a row.
/// The MediatR <c>AuditingBehavior</c> nests INSIDE this scope, attaching command/query
/// details — but the request-level scope is always submitted on its own.
/// </summary>
public sealed class AuditScopeMiddleware
{
    private const string CorrelationItemKey = "CorrelationId";
    private readonly RequestDelegate _next;
    private readonly AuditingOptions _options;

    public AuditScopeMiddleware(RequestDelegate next, IOptions<AuditingOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task Invoke(HttpContext context,
        IAuditScopeAccessor accessor,
        IAuditWriter writer,
        ICurrentUser currentUser,
        ITenantContext tenant)
    {
        if (!_options.Enabled || ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var scope = accessor.Push($"Http:{context.Request.Method}:{context.Request.Path}");
        scope.HttpMethod = context.Request.Method;
        scope.Url = context.Request.Path + context.Request.QueryString;
        scope.ClientIp = context.Connection.RemoteIpAddress?.ToString();
        scope.BrowserInfo = context.Request.Headers.UserAgent.ToString();
        scope.CorrelationId = context.Items.TryGetValue(CorrelationItemKey, out var cid) ? cid?.ToString() : null;
        scope.UserId = currentUser.UserId;
        scope.UserName = currentUser.UserName ?? (currentUser.IsAuthenticated ? null : _options.AnonymousUserName);
        scope.TenantId = tenant.Current?.Id ?? currentUser.TenantId;

        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
            sw.Stop();
            scope.DurationMs = (int)sw.ElapsedMilliseconds;
            scope.HttpStatusCode = context.Response.StatusCode;
            await writer.WriteAsync(scope, context.RequestAborted);
        }
        catch (Exception ex)
        {
            sw.Stop();
            scope.DurationMs = (int)sw.ElapsedMilliseconds;
            scope.Exception = ex.ToString();
            scope.HttpStatusCode = context.Response.HasStarted ? context.Response.StatusCode : StatusCodes.Status500InternalServerError;
            if (_options.AuditFailures)
                await writer.WriteAsync(scope, CancellationToken.None);
            throw;
        }
        finally
        {
            accessor.Pop(scope);
        }
    }

    private static bool ShouldSkip(PathString path) =>
        path.StartsWithSegments("/health") ||
        path.StartsWithSegments("/swagger") ||
        path.StartsWithSegments("/scalar") ||
        path.StartsWithSegments("/openapi") ||
        path.StartsWithSegments("/_framework");
}
