using System.Diagnostics;
using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Auditing.Abstractions;
using BuildingBlocks.Auditing.Sanitisation;
using BuildingBlocks.MultiTenancy;
using MediatR;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Auditing.Behaviors;

/// <summary>
/// MediatR behavior that brackets every command (and optionally query) with an audit scope.
/// On completion, populates the scope with caller / timing / exception info and submits
/// it to the writer — fire-and-forget.
///
/// Sits OUTSIDE the unit of work behavior so it can capture both successes and failures.
/// </summary>
public sealed class AuditingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAuditScopeAccessor _scopeAccessor;
    private readonly IAuditWriter _writer;
    private readonly ICurrentUser _currentUser;
    private readonly ITenantContext _tenant;
    private readonly AuditingOptions _options;

    public AuditingBehavior(
        IAuditScopeAccessor scopeAccessor,
        IAuditWriter writer,
        ICurrentUser currentUser,
        ITenantContext tenant,
        IOptions<AuditingOptions> options)
    {
        _scopeAccessor = scopeAccessor;
        _writer = writer;
        _currentUser = currentUser;
        _tenant = tenant;
        _options = options.Value;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
            return await next();

        var isCommand = IsCommand(typeof(TRequest));
        if (!isCommand && !_options.AuditQueries)
            return await next();

        var action = $"{(isCommand ? "Command" : "Query")}:{typeof(TRequest).Name}";
        var scope = _scopeAccessor.Push(action);
        scope.ServiceName = typeof(TRequest).FullName;
        scope.MethodName = nameof(IRequestHandler<TRequest, TResponse>.Handle);
        scope.Parameters = ParameterSanitiser.ToJson(request, _options);
        scope.UserId = _currentUser.UserId;
        scope.UserName = _currentUser.UserName ?? (_currentUser.IsAuthenticated ? null : _options.AnonymousUserName);
        scope.TenantId = _tenant.Current?.Id ?? _currentUser.TenantId;

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next();
            sw.Stop();
            scope.DurationMs = (int)sw.ElapsedMilliseconds;
            // ReturnValue intentionally omitted — handlers often return Result<T> with PII.
            await _writer.WriteAsync(scope, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            scope.DurationMs = (int)sw.ElapsedMilliseconds;
            scope.Exception = ex.ToString();
            if (_options.AuditFailures)
                await _writer.WriteAsync(scope, cancellationToken);
            throw;
        }
        finally
        {
            _scopeAccessor.Pop(scope);
        }
    }

    private static bool IsCommand(Type t)
    {
        if (typeof(ICommand).IsAssignableFrom(t)) return true;
        foreach (var i in t.GetInterfaces())
            if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>))
                return true;
        return false;
    }
}
