using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.Application.UnitOfWork;
using MediatR;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// Wraps every command in an ambient Unit of Work. Queries are not wrapped — they're
/// expected to be read-only and use no-tracking projections.
/// Nested UoWs participate in the outer transaction (see <see cref="IUnitOfWorkManager"/>).
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWorkManager _uowManager;

    public UnitOfWorkBehavior(IUnitOfWorkManager uowManager) => _uowManager = uowManager;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!IsCommand())
            return await next();

        await using var uow = _uowManager.Begin();
        var response = await next();
        await uow.CompleteAsync(cancellationToken);
        return response;
    }

    private static bool IsCommand()
    {
        var type = typeof(TRequest);
        if (typeof(ICommand).IsAssignableFrom(type))
            return true;

        foreach (var i in type.GetInterfaces())
        {
            if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>))
                return true;
        }
        return false;
    }
}
