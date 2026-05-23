using BuildingBlocks.Application;
using BuildingBlocks.Application.Modules;
using BuildingBlocks.EventBus;
using BuildingBlocks.Infrastructure.Outbox;
using BuildingBlocks.Infrastructure.Persistence.Interceptors;
using BuildingBlocks.Application.UnitOfWork;
using Customers.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orders.Application;
using Orders.Application.Orders.EventHandlers;
using Orders.Application.Orders.Queries;
using Orders.Application.Orders.Queries.GetOrderById;
using Orders.Contracts;
using Orders.Domain.Orders;
using Orders.Infrastructure.Persistence;
using Orders.Infrastructure.Persistence.Repositories;
using Orders.Infrastructure.PublicApi;

namespace Orders.Infrastructure;

public sealed class OrdersModule : IModule
{
    public string Name => "Orders";
    public string DatabaseSchema => OrdersDbContext.SchemaName;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing connection string 'Postgres'.");

        services.AddScoped<OutboxAccumulator>();
        services.AddScoped<IIntegrationEventQueue>(sp => sp.GetRequiredService<OutboxAccumulator>());

        services.AddDbContext<OrdersDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npg =>
            {
                npg.MigrationsHistoryTable("__ef_migrations_history", OrdersDbContext.SchemaName);
                npg.MigrationsAssembly(typeof(OrdersDbContext).Assembly.FullName);
            });
            options.UseSnakeCaseNamingConvention();
            options.AddInterceptors(
                sp.GetRequiredService<AuditingInterceptor>(),
                sp.GetRequiredService<SoftDeleteInterceptor>(),
                sp.GetRequiredService<OutboxInterceptor>());
        });

        services.TryAddScoped<AuditingInterceptor>();
        services.TryAddScoped<SoftDeleteInterceptor>();
        services.AddScoped<OutboxInterceptor>();

        // Register this module's DbContext as a committer for the UoW pipeline behavior
        services.AddScoped<IUnitOfWorkCommitter>(sp => sp.GetRequiredService<OrdersDbContext>());

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderReadModel, OrderReadModel>();
        services.AddScoped<IOrderLookupForCustomer, OrderLookupForCustomer>();
        services.AddScoped<IOrdersApi, OrdersApi>();

        // Cross-module integration event subscription.
        services.AddScoped<IIntegrationEventHandler<CustomerDeactivatedIntegrationEvent>, CustomerDeactivatedHandler>();

        services.AddApplicationPipeline(
            typeof(OrderPermissions).Assembly,
            typeof(OrdersModule).Assembly);

        services.AddHostedService<BuildingBlocks.Infrastructure.Outbox.OutboxProcessor<OrdersDbContext>>();
    }
}
