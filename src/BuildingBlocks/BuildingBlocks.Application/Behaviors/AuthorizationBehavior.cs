using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Application.Security;
using BuildingBlocks.SharedKernel;
using MediatR;
using System.Reflection;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// Inspects the request for <see cref="RequiresPermissionAttribute"/> declarations and
/// short-circuits with an Unauthorized/Forbidden result if the current principal lacks them.
/// </summary>
public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionService _permissionService;

    public AuthorizationBehavior(ICurrentUser currentUser, IPermissionService permissionService)
    {
        _currentUser = currentUser;
        _permissionService = permissionService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var permissions = typeof(TRequest)
            .GetCustomAttributes<RequiresPermissionAttribute>()
            .Select(a => a.Permission)
            .ToArray();

        if (permissions.Length == 0)
            return await next();

        if (!_currentUser.IsAuthenticated)
            return Failure(Error.Unauthorized("Auth.NotAuthenticated", "Authentication is required."));

        foreach (var permission in permissions)
        {
            if (!await _permissionService.HasPermissionAsync(_currentUser.UserId!.Value, permission, cancellationToken))
                return Failure(Error.Forbidden("Auth.Forbidden", $"Missing permission: {permission}"));
        }

        return await next();
    }

    private static TResponse Failure(Error error)
    {
        var responseType = typeof(TResponse);
        if (responseType == typeof(Result))
            return (TResponse)(object)Result.Failure(error);
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = typeof(Result)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(m => m.Name == nameof(Result.Failure) && m.IsGenericMethod);
            var generic = failureMethod.MakeGenericMethod(responseType.GetGenericArguments()[0]);
            return (TResponse)generic.Invoke(null, [error])!;
        }
        throw new InvalidOperationException($"Unsupported response type {responseType.Name}.");
    }
}
