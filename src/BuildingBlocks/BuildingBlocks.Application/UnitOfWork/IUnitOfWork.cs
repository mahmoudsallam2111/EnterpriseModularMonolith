using System.Data;

namespace BuildingBlocks.Application.UnitOfWork;

/// <summary>
/// A logical unit of work. Disposing without calling <see cref="CompleteAsync"/> rolls back.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    Guid Id { get; }
    UnitOfWorkOptions Options { get; }
    bool IsCompleted { get; }
    bool IsRolledBack { get; }
    IUnitOfWork? Outer { get; }

    Task CompleteAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Register a callback to be invoked after the unit of work commits successfully.
    /// Used by the event dispatcher to publish integration events post-commit.
    /// </summary>
    void OnCompleted(Func<CancellationToken, Task> callback);
}

public sealed record UnitOfWorkOptions(
    bool RequiresNew = false,
    IsolationLevel IsolationLevel = IsolationLevel.ReadCommitted,
    TimeSpan? Timeout = null);

/// <summary>
/// Ambient access to the currently active unit of work. Implemented with AsyncLocal so
/// nested handlers and pipeline behaviors always observe the same instance.
/// </summary>
public interface IUnitOfWorkAccessor
{
    IUnitOfWork? Current { get; }
}

/// <summary>
/// Factory and ambient context coordinator for units of work. Modelled after ABP's
/// UnitOfWorkManager — call <see cref="Begin"/> to start one; nested calls automatically
/// reuse the outer UoW unless <c>RequiresNew</c> is specified.
/// </summary>
public interface IUnitOfWorkManager : IUnitOfWorkAccessor
{
    IUnitOfWork Begin(UnitOfWorkOptions? options = null);
}
