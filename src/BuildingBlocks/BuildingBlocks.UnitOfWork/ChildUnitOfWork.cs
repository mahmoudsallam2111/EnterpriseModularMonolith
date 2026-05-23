using BuildingBlocks.Application.UnitOfWork;

namespace BuildingBlocks.UnitOfWork;

/// <summary>
/// Nested unit of work — does not commit on its own. Completion just bubbles up
/// any registered post-commit callbacks to the outer (root) unit of work.
/// </summary>
internal sealed class ChildUnitOfWork : IUnitOfWork
{
    private readonly AmbientUnitOfWorkAccessor _accessor;
    private readonly List<Func<CancellationToken, Task>> _onCompleted = [];

    public ChildUnitOfWork(IUnitOfWork outer, UnitOfWorkOptions options, AmbientUnitOfWorkAccessor accessor)
    {
        Outer = outer;
        Options = options;
        _accessor = accessor;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public UnitOfWorkOptions Options { get; }
    public bool IsCompleted { get; private set; }
    public bool IsRolledBack { get; private set; }
    public IUnitOfWork? Outer { get; }

    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        IsCompleted = true;
        foreach (var cb in _onCompleted)
            Outer?.OnCompleted(cb);
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        IsRolledBack = true;
        return Outer?.RollbackAsync(cancellationToken) ?? Task.CompletedTask;
    }

    public void OnCompleted(Func<CancellationToken, Task> callback) => _onCompleted.Add(callback);

    public ValueTask DisposeAsync()
    {
        _accessor.Pop(this);
        return ValueTask.CompletedTask;
    }
}
