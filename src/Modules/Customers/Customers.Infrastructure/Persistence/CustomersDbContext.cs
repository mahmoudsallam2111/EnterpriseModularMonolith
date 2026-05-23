using BuildingBlocks.EventBus;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Customers.Domain.Customers;

namespace Customers.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Customers module. Lives in its own "customers" schema —
/// no other module's DbContext sees these tables.
/// </summary>
public sealed class CustomersDbContext : ModuleDbContext
{
    public const string SchemaName = "customers";
    public override string Schema => SchemaName;

    public DbSet<Customer> Customers => Set<Customer>();

    public CustomersDbContext(
        DbContextOptions<CustomersDbContext> options,
        IDomainEventDispatcher domainEventDispatcher,
        ILogger<CustomersDbContext> logger)
        : base(options, domainEventDispatcher, logger) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustomersDbContext).Assembly);
    }
}
