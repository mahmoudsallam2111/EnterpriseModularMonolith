using BuildingBlocks.Application.DataFiltering;
using BuildingBlocks.EventBus;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Persistence;

public sealed class OrdersDbContext : ModuleDbContext
{
    public const string SchemaName = "orders";
    public override string Schema => SchemaName;

    public DbSet<Order> Orders => Set<Order>();

    public OrdersDbContext(
        DbContextOptions<OrdersDbContext> options,
        IDomainEventDispatcher domainEventDispatcher,
        IDataFilter dataFilter,
        ITenantContext tenantContext,
        ILogger<OrdersDbContext> logger)
        : base(options, domainEventDispatcher, dataFilter, tenantContext, logger) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
