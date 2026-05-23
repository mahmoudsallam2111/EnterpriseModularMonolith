using System.Data;
using BuildingBlocks.Application.UnitOfWork;
using BuildingBlocks.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.UnitOfWork;

/// <summary>
/// Root UoW backed by an EF Core transaction. Wires SaveChanges + Commit + post-commit callbacks
/// (used by the outbox processor / integration event dispatch).
/// </summary>
public sealed class EfCoreUnitOfWork<TDbContext> : IUnitOfWork
    where TDbContext : DbContext
{
    private readonly TDbContext _db;
    private readonly AmbientUnitOfWorkAccessor _accessor;
    private readonly ILogger<EfCoreUnitOfWork<TDbContext>> _logger;
    private readonly List<Func<CancellationToken, Task>> _onCompleted = [];
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public EfCoreUnitOfWork(
        TDbContext db,
        UnitOfWorkOptions options,
        AmbientUnitOfWorkAccessor accessor,
        IUnitOfWork? outer,
        ILogger<EfCoreUnitOfWork<TDbContext>> logger)
    {
        _db = db;
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

    public void OnCompleted(Func<CancellationToken, Task> callback) => _onCompleted.Add(callback);

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (IsCompleted) return;

        _transaction ??= await _db.Database.BeginTransactionAsync(Options.IsolationLevel, cancellationToken);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
            IsCompleted = true;
            _logger.LogDebug("UoW {UoWId} committed", Id);
        }
        catch
        {
            await SafeRollbackAsync(cancellationToken);
            throw;
        }

        // Fire post-commit callbacks (integration event publishing happens here).
        foreach (var cb in _onCompleted)
        {
            try { await cb(cancellationToken); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Post-commit callback failed for UoW {UoWId}", Id);
            }
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await SafeRollbackAsync(cancellationToken);
        IsRolledBack = true;
    }

    private async Task SafeRollbackAsync(CancellationToken cancellationToken)
    {
        if (_transaction is not null)
        {
            try { await _transaction.RollbackAsync(cancellationToken); }
            catch (Exception ex) { _logger.LogWarning(ex, "Rollback failed for UoW {UoWId}", Id); }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_transaction is not null)
            await _transaction.DisposeAsync();

        _accessor.Pop(this);
    }
}

public sealed class EfCoreUnitOfWorkFactory<TDbContext> : IUnitOfWorkFactory
    where TDbContext : DbContext
{
    private readonly IServiceProvider _serviceProvider;

    public EfCoreUnitOfWorkFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public IUnitOfWork Create(UnitOfWorkOptions options, AmbientUnitOfWorkAccessor accessor, IUnitOfWork? outer)
    {
        var db = (TDbContext)_serviceProvider.GetService(typeof(TDbContext))!;
        var logger = (ILogger<EfCoreUnitOfWork<TDbContext>>)
            _serviceProvider.GetService(typeof(ILogger<EfCoreUnitOfWork<TDbContext>>))!;
        return new EfCoreUnitOfWork<TDbContext>(db, options, accessor, outer, logger);
    }
}
