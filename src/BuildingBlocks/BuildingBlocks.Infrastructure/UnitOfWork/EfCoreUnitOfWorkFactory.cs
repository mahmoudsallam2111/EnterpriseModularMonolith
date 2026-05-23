using BuildingBlocks.Application.UnitOfWork;
using BuildingBlocks.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.UnitOfWork;

public sealed class EfCoreUnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IEnumerable<IUnitOfWorkCommitter> _committers;
    private readonly ILogger<EfCoreUnitOfWork> _logger;

    public EfCoreUnitOfWorkFactory(
        IEnumerable<IUnitOfWorkCommitter> committers,
        ILogger<EfCoreUnitOfWork> logger)
    {
        _committers = committers;
        _logger = logger;
    }

    public IUnitOfWork Create(UnitOfWorkOptions options, AmbientUnitOfWorkAccessor accessor, IUnitOfWork? outer)
    {
        return EfCoreUnitOfWork.Begin(_committers.ToArray(), options, accessor, outer, _logger);
    }

    public async Task<IUnitOfWork> CreateAsync(
        UnitOfWorkOptions options,
        AmbientUnitOfWorkAccessor accessor,
        IUnitOfWork? outer,
        CancellationToken cancellationToken = default)
    {
        return await EfCoreUnitOfWork.BeginAsync(
            _committers.ToArray(),
            options,
            accessor,
            outer,
            _logger,
            cancellationToken);
    }
}
