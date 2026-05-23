using BuildingBlocks.Application.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.UnitOfWork;

/// <summary>
/// Coordinates units of work. Each <see cref="Begin"/> call returns a UoW that
/// participates in the outer one unless <c>RequiresNew</c> is set; that way
/// nested handlers don't need to know who started the outermost transaction.
/// </summary>
public sealed class UnitOfWorkManager : IUnitOfWorkManager
{
    private readonly AmbientUnitOfWorkAccessor _accessor;
    private readonly IServiceProvider _serviceProvider;

    public UnitOfWorkManager(AmbientUnitOfWorkAccessor accessor, IServiceProvider serviceProvider)
    {
        _accessor = accessor;
        _serviceProvider = serviceProvider;
    }

    public IUnitOfWork? Current => _accessor.Current;

    public IUnitOfWork Begin(UnitOfWorkOptions? options = null)
    {
        return BeginAsync(options).GetAwaiter().GetResult();
    }

    public async Task<IUnitOfWork> BeginAsync(
        UnitOfWorkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new UnitOfWorkOptions();
        var outer = _accessor.Current;

        if (outer is not null && !options.RequiresNew)
        {
            var child = new ChildUnitOfWork(outer, options, _accessor);
            _accessor.Push(child);
            return child;
        }

        var factory = _serviceProvider.GetRequiredService<IUnitOfWorkFactory>();
        var root = await factory.CreateAsync(options, _accessor, outer, cancellationToken);
        _accessor.Push(root);
        return root;
    }
}

/// <summary>
/// Hook used by the infrastructure (EF Core) layer to create the real root UoW.
/// Splits the abstraction from the EF dependency in the BuildingBlocks ring.
/// </summary>
public interface IUnitOfWorkFactory
{
    IUnitOfWork Create(UnitOfWorkOptions options, AmbientUnitOfWorkAccessor accessor, IUnitOfWork? outer);
    Task<IUnitOfWork> CreateAsync(
        UnitOfWorkOptions options,
        AmbientUnitOfWorkAccessor accessor,
        IUnitOfWork? outer,
        CancellationToken cancellationToken = default);
}
