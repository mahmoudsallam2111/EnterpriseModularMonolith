namespace BuildingBlocks.Application.UnitOfWork;

/// <summary>
/// Abstraction for persisting tracked changes. Each module registers its own
/// implementation (backed by its DbContext). The pipeline behavior resolves all
/// registered committers and calls <see cref="SaveChangesAsync"/> on those that
/// have pending work.
/// </summary>
public interface IUnitOfWorkCommitter
{
    bool HasChanges();
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
