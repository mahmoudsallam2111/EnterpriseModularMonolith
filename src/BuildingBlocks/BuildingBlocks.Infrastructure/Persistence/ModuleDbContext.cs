using BuildingBlocks.Application.UnitOfWork;
using BuildingBlocks.Domain;
using BuildingBlocks.EventBus;
using BuildingBlocks.EventBus.Outbox;
using BuildingBlocks.Infrastructure.Persistence.Interceptors;
using BuildingBlocks.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Base DbContext that every module derives from. It enforces a per-module schema,
/// applies the standard interceptors (auditing, soft delete), and exposes a
/// SaveChanges path that dispatches domain events before commit and persists
/// outbox messages alongside the aggregate change.
/// </summary>
public abstract class ModuleDbContext : DbContext
{
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly ILogger _logger;

    /// <summary>Schema this module's tables live in (e.g. "customers").</summary>
    public abstract string Schema { get; }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected ModuleDbContext(
        DbContextOptions options,
        IDomainEventDispatcher domainEventDispatcher,
        ILogger logger)
        : base(options)
    {
        _domainEventDispatcher = domainEventDispatcher;
        _logger = logger;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("outbox_messages", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).IsRequired().HasMaxLength(512);
            b.Property(x => x.Payload).IsRequired();
            b.HasIndex(x => x.ProcessedOnUtc);
            b.HasIndex(x => x.OccurredOnUtc);
        });

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 1. Collect domain events before save so we can dispatch them in-transaction.
        var aggregatesWithEvents = ChangeTracker.Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToArray();

        var domainEvents = aggregatesWithEvents
            .SelectMany(a => a.DomainEvents)
            .ToArray();

        foreach (var agg in aggregatesWithEvents)
            agg.ClearDomainEvents();

        if (domainEvents.Length > 0)
        {
            _logger.LogDebug("Dispatching {Count} domain event(s) before commit", domainEvents.Length);
            await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
