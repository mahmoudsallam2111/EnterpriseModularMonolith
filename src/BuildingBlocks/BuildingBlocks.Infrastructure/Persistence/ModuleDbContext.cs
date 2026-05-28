using System.Linq.Expressions;
using System.Reflection;
using BuildingBlocks.Application.DataFiltering;
using BuildingBlocks.Application.UnitOfWork;
using BuildingBlocks.Domain;
using BuildingBlocks.EventBus;
using BuildingBlocks.EventBus.Outbox;
using BuildingBlocks.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Base DbContext that every module derives from. Responsibilities:
///  • per-module schema,
///  • automatic global query filters for any entity implementing
///    <see cref="ISoftDeletable"/> or <see cref="IMultiTenantEntity"/> — gated by
///    <see cref="IDataFilter"/> so they can be toggled per-scope at runtime,
///  • dispatch of domain events before commit,
///  • outbox persistence inside the same transaction (via interceptor).
/// </summary>
public abstract class ModuleDbContext : DbContext, IUnitOfWorkCommitter
{
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IDataFilter _dataFilter;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger _logger;

    /// <summary>Schema this module's tables live in (e.g. "customers").</summary>
    public abstract string Schema { get; }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>
    /// Read by the auto-applied global query filter. EF Core captures property access
    /// on the DbContext as a parameter, re-reading it on every query — that's what
    /// makes the filter togglable via <see cref="IDataFilter"/> without rebuilding the model.
    /// </summary>
    protected bool IsSoftDeleteFilterEnabled => _dataFilter.IsEnabled<ISoftDeletable>();

    /// <summary>Same trick for the tenant filter.</summary>
    protected bool IsMultiTenantFilterEnabled => _dataFilter.IsEnabled<IMultiTenantEntity>();

    /// <summary>Tenant id used by the multi-tenant filter. Null when no tenant resolved.</summary>
    protected Guid? CurrentTenantId => _tenantContext.Current?.Id;

    protected ModuleDbContext(
        DbContextOptions options,
        IDomainEventDispatcher domainEventDispatcher,
        IDataFilter dataFilter,
        ITenantContext tenantContext,
        ILogger logger)
        : base(options)
    {
        _domainEventDispatcher = domainEventDispatcher;
        _dataFilter = dataFilter;
        _tenantContext = tenantContext;
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

        // Auto-apply global query filters AFTER module configurations have run so
        // we see every entity type the module registered.
        ConfigureAutomaticGlobalFilters(modelBuilder);
    }

    private void ConfigureAutomaticGlobalFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Skip owned types — EF cannot apply query filters on them; the filter on the
            // owning root entity already excludes the dependent rows.
            if (entityType.IsOwned()) continue;
            if (entityType.ClrType is null) continue;

            var isSoftDeletable = typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType);
            var isMultiTenant = typeof(IMultiTenantEntity).IsAssignableFrom(entityType.ClrType);
            if (!isSoftDeletable && !isMultiTenant) continue;

            var filterExpression = BuildFilterExpression(entityType.ClrType, isSoftDeletable, isMultiTenant);
            if (filterExpression is null) continue;

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filterExpression);
        }
    }

    private LambdaExpression? BuildFilterExpression(Type clrType, bool isSoftDeletable, bool isMultiTenant)
    {
        var parameter = Expression.Parameter(clrType, "e");
        Expression? body = null;

        if (isSoftDeletable)
        {
            // !IsSoftDeleteFilterEnabled || !e.IsDeleted
            var filterEnabledExpr = Expression.Property(Expression.Constant(this), nameof(IsSoftDeleteFilterEnabled));
            var isDeletedExpr = Expression.Property(Expression.Convert(parameter, typeof(ISoftDeletable)), nameof(ISoftDeletable.IsDeleted));
            body = Expression.OrElse(Expression.Not(filterEnabledExpr), Expression.Not(isDeletedExpr));
        }

        if (isMultiTenant)
        {
            // !IsMultiTenantFilterEnabled || CurrentTenantId == null || e.TenantId == CurrentTenantId
            var filterEnabledExpr = Expression.Property(Expression.Constant(this), nameof(IsMultiTenantFilterEnabled));
            var tenantIdExpr = Expression.Property(Expression.Convert(parameter, typeof(IMultiTenantEntity)), nameof(IMultiTenantEntity.TenantId));
            var currentTenantExpr = Expression.Property(Expression.Constant(this), nameof(CurrentTenantId));
            var nullableGuidType = typeof(Guid?);

            var notFiltering = Expression.Not(filterEnabledExpr);
            var currentNull = Expression.Equal(currentTenantExpr, Expression.Constant(null, nullableGuidType));
            var sameTenant = Expression.Equal(tenantIdExpr, currentTenantExpr);

            var clause = Expression.OrElse(Expression.OrElse(notFiltering, currentNull), sameTenant);
            body = body is null ? clause : Expression.AndAlso(body, clause);
        }

        return body is null ? null : Expression.Lambda(body, parameter);
    }

    public bool HasChanges() => ChangeTracker.HasChanges();

    async Task IUnitOfWorkCommitter.SaveChangesAsync(CancellationToken cancellationToken)
    {
        await SaveChangesAsync(cancellationToken);
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
