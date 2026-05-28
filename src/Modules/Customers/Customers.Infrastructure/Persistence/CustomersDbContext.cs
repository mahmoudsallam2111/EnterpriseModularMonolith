using BuildingBlocks.Application.DataFiltering;
using BuildingBlocks.EventBus;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Customers.Domain.Customers;

namespace Customers.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Customers module. Lives in its own "customers" schema —
/// no other module's DbContext sees these tables. Global query filters for
/// ISoftDeletable / IMultiTenantEntity are applied automatically by the base
/// <see cref="ModuleDbContext"/>; no per-entity HasQueryFilter calls are needed.
/// </summary>
public sealed class CustomersDbContext : ModuleDbContext
{
    public const string SchemaName = "customers";
    public override string Schema => SchemaName;

    public DbSet<Customer> Customers => Set<Customer>();

    public CustomersDbContext(
        DbContextOptions<CustomersDbContext> options,
        IDomainEventDispatcher domainEventDispatcher,
        IDataFilter dataFilter,
        ITenantContext tenantContext,
        ILogger<CustomersDbContext> logger)
        : base(options, domainEventDispatcher, dataFilter, tenantContext, logger) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustomersDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
