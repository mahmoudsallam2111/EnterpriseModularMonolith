using BuildingBlocks.Application.UnitOfWork;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.UnitOfWork;

/// <summary>
/// Request-scoped EF Core unit of work shared by every module DbContext in the scope.
/// All module contexts enlist in one database transaction and commit together.
/// </summary>
public sealed class EfCoreUnitOfWork : IUnitOfWork
{
    private const int MaxSavePasses = 8;

    private readonly IReadOnlyList<IUnitOfWorkCommitter> _committers;
    private readonly IReadOnlyList<ModuleDbContext> _contexts;
    private readonly AmbientUnitOfWorkAccessor _accessor;
    private readonly ILogger<EfCoreUnitOfWork> _logger;
    private readonly List<Func<CancellationToken, Task>> _onCompleted = [];
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    private EfCoreUnitOfWork(
        IReadOnlyList<IUnitOfWorkCommitter> committers,
        IReadOnlyList<ModuleDbContext> contexts,
        UnitOfWorkOptions options,
        AmbientUnitOfWorkAccessor accessor,
        IUnitOfWork? outer,
        ILogger<EfCoreUnitOfWork> logger)
    {
        _committers = committers;
        _contexts = contexts;
        Options = options;
        Outer = outer;
        _accessor = accessor;
        _logger = logger;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public UnitOfWorkOptions Options { get; }
    public bool IsCompleted { get; private set; }
    public bool IsRolledBack { get; private set; }
    public IUnitOfWork? Outer { get; }

    public static EfCoreUnitOfWork Begin(
        IReadOnlyList<IUnitOfWorkCommitter> committers,
        UnitOfWorkOptions options,
        AmbientUnitOfWorkAccessor accessor,
        IUnitOfWork? outer,
        ILogger<EfCoreUnitOfWork> logger)
    {
        var contexts = GetModuleContexts(committers);
        var unitOfWork = new EfCoreUnitOfWork(committers, contexts, options, accessor, outer, logger);
        unitOfWork.BeginTransaction();
        return unitOfWork;
    }

    public static async Task<EfCoreUnitOfWork> BeginAsync(
        IReadOnlyList<IUnitOfWorkCommitter> committers,
        UnitOfWorkOptions options,
        AmbientUnitOfWorkAccessor accessor,
        IUnitOfWork? outer,
        ILogger<EfCoreUnitOfWork> logger,
        CancellationToken cancellationToken)
    {
        var contexts = GetModuleContexts(committers);
        var unitOfWork = new EfCoreUnitOfWork(committers, contexts, options, accessor, outer, logger);
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        return unitOfWork;
    }

    public void OnCompleted(Func<CancellationToken, Task> callback) => _onCompleted.Add(callback);

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (IsCompleted) return;

        try
        {
            await SaveChangesUntilCleanAsync(cancellationToken);

            if (_transaction is not null)
            {
                await _transaction.CommitAsync(cancellationToken);
            }

            IsCompleted = true;
            _logger.LogDebug("UoW {UoWId} committed", Id);
        }
        catch
        {
            await SafeRollbackAsync(CancellationToken.None);
            throw;
        }

        foreach (var callback in _onCompleted)
        {
            try { await callback(cancellationToken); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Post-commit callback failed for UoW {UoWId}", Id);
            }
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (IsCompleted || IsRolledBack) return;

        await SafeRollbackAsync(cancellationToken);
        IsRolledBack = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (!IsCompleted && !IsRolledBack)
        {
            await SafeRollbackAsync(CancellationToken.None);
        }

        ClearEnlistedTransactions();

        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
        }

        _accessor.Pop(this);
    }

    private void BeginTransaction()
    {
        if (_contexts.Count == 0) return;

        _transaction = _contexts[0].Database.BeginTransaction(Options.IsolationLevel);
        var dbTransaction = _transaction.GetDbTransaction();

        for (var i = 1; i < _contexts.Count; i++)
        {
            _contexts[i].Database.UseTransaction(dbTransaction);
        }
    }

    private async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (_contexts.Count == 0) return;

        _transaction = await _contexts[0].Database.BeginTransactionAsync(Options.IsolationLevel, cancellationToken);
        var dbTransaction = _transaction.GetDbTransaction();

        for (var i = 1; i < _contexts.Count; i++)
        {
            await _contexts[i].Database.UseTransactionAsync(dbTransaction, cancellationToken);
        }
    }

    private async Task SaveChangesUntilCleanAsync(CancellationToken cancellationToken)
    {
        for (var pass = 0; pass < MaxSavePasses; pass++)
        {
            var savedAny = false;

            foreach (var committer in _committers)
            {
                if (!committer.HasChanges()) continue;

                await committer.SaveChangesAsync(cancellationToken);
                savedAny = true;
            }

            if (!savedAny) return;
        }

        throw new InvalidOperationException("Unit of work still has pending changes after repeated save attempts.");
    }

    private async Task SafeRollbackAsync(CancellationToken cancellationToken)
    {
        if (_transaction is null) return;

        try { await _transaction.RollbackAsync(cancellationToken); }
        catch (Exception ex) { _logger.LogWarning(ex, "Rollback failed for UoW {UoWId}", Id); }
    }

    private void ClearEnlistedTransactions()
    {
        for (var i = 1; i < _contexts.Count; i++)
        {
            _contexts[i].Database.UseTransaction(null);
        }
    }

    private static ModuleDbContext[] GetModuleContexts(IEnumerable<IUnitOfWorkCommitter> committers) =>
        committers.OfType<ModuleDbContext>().Distinct().ToArray();
}
