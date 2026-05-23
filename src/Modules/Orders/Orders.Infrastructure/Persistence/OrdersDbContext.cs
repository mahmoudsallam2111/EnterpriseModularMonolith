using BuildingBlocks.EventBus;
using BuildingBlocks.Infrastructure.Persistence;
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
        ILogger<OrdersDbContext> logger)
        : base(options, domainEventDispatcher, logger) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
    }
}
