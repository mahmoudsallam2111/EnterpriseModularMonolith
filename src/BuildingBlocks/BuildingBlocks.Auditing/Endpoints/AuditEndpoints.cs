using BuildingBlocks.Auditing.Entities;
using BuildingBlocks.Auditing.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Auditing.Endpoints;

/// <summary>
/// Admin endpoints for browsing the audit log. Mount with
/// <c>app.MapAuditEndpoints()</c> — gated behind a permission of your choice.
/// </summary>
public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/audit")
            .WithTags("Audit")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGet("/logs", ListLogsAsync)
            .WithName("ListAuditLogs")
            .WithSummary("Paged search over the audit log.");

        group.MapGet("/logs/{id:guid}", GetLogAsync)
            .WithName("GetAuditLog")
            .WithSummary("Get a single audit log with all entity changes and property diffs.");

        group.MapGet("/entities/{entityType}/{entityId}/history", EntityHistoryAsync)
            .WithName("EntityAuditHistory")
            .WithSummary("Full change history for one entity (most useful endpoint).");

        return endpoints;
    }

    private static async Task<IResult> ListLogsAsync(
        AuditDbContext db,
        CancellationToken cancellationToken,
        Guid? userId = null,
        string? action = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        string? correlationId = null,
        int page = 1,
        int pageSize = 50)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (userId is { } u) query = query.Where(x => x.UserId == u);
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.Action == action);
        if (from is { } f) query = query.Where(x => x.ExecutionTime >= f);
        if (to is { } t) query = query.Where(x => x.ExecutionTime <= t);
        if (!string.IsNullOrWhiteSpace(correlationId)) query = query.Where(x => x.CorrelationId == correlationId);

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.ExecutionTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogListItem(
                x.Id, x.Action, x.UserId, x.UserName, x.HttpMethod, x.Url,
                x.HttpStatusCode, x.ExecutionTime, x.ExecutionDurationMs, x.Exception != null))
            .ToListAsync(cancellationToken);

        return Results.Ok(new { items, page, pageSize, total });
    }

    private static async Task<IResult> GetLogAsync(Guid id, AuditDbContext db, CancellationToken cancellationToken)
    {
        var log = await db.AuditLogs
            .AsNoTracking()
            .Include(l => l.EntityChanges).ThenInclude(c => c.PropertyChanges)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        return log is null ? Results.NotFound() : Results.Ok(log);
    }

    private static async Task<IResult> EntityHistoryAsync(
        string entityType,
        string entityId,
        AuditDbContext db,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 50)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = db.EntityChanges.AsNoTracking()
            .Where(c => c.EntityType == entityType && c.EntityId == entityId);

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.ChangeTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.PropertyChanges)
            .ToListAsync(cancellationToken);

        return Results.Ok(new { items, page, pageSize, total });
    }

    private sealed record AuditLogListItem(
        Guid Id,
        string Action,
        Guid? UserId,
        string? UserName,
        string? HttpMethod,
        string? Url,
        int? HttpStatusCode,
        DateTimeOffset ExecutionTime,
        int ExecutionDurationMs,
        bool HadException);
}
