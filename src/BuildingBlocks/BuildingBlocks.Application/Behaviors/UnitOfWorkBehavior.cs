using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.Application.UnitOfWork;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// Saves command changes when there is no ambient request unit of work.
/// Queries are not wrapped — they're expected to be read-only and use no-tracking projections.
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IUnitOfWorkCommitter> _committers;
    private readonly IUnitOfWorkAccessor _unitOfWorkAccessor;
    private readonly IServiceProvider _serviceProvider;

    public UnitOfWorkBehavior(
        IEnumerable<IUnitOfWorkCommitter> committers,
        IUnitOfWorkAccessor unitOfWorkAccessor,
        IServiceProvider serviceProvider)
    {
        _committers = committers;
        _unitOfWorkAccessor = unitOfWorkAccessor;
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!IsCommand())
            return await next();

        if (_unitOfWorkAccessor.Current is not null)
            return await next();

        var unitOfWorkManager = _serviceProvider.GetService<IUnitOfWorkManager>();
        if (unitOfWorkManager is not null)
        {
            await using var unitOfWork = await unitOfWorkManager.BeginAsync(cancellationToken: cancellationToken);
            var result = await next();
            await unitOfWork.CompleteAsync(cancellationToken);
            return result;
        }

        var response = await next();
        await SaveChangedCommittersAsync(cancellationToken);

        return response;
    }

    private async Task SaveChangedCommittersAsync(CancellationToken cancellationToken)
    {
        foreach (var committer in _committers)
        {
            if (committer.HasChanges())
            {
                await committer.SaveChangesAsync(cancellationToken);
            }
        }
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
