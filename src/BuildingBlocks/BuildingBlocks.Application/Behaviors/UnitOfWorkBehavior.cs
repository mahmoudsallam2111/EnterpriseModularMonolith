using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.Application.UnitOfWork;
using MediatR;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// Wraps every command in an implicit save. After the handler runs, any registered
/// <see cref="IUnitOfWorkCommitter"/> that has pending changes gets saved.
/// Queries are not wrapped — they're expected to be read-only and use no-tracking projections.
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IUnitOfWorkCommitter> _committers;

    public UnitOfWorkBehavior(IEnumerable<IUnitOfWorkCommitter> committers) => _committers = committers;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!IsCommand())
            return await next();

        var response = await next();

        // Save changes on every module DbContext that has pending modifications.
        foreach (var committer in _committers)
        {
            if (committer.HasChanges())
            {
                await committer.SaveChangesAsync(cancellationToken);
            }
        }

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
