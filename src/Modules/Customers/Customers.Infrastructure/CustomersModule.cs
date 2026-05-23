using BuildingBlocks.Application;
using BuildingBlocks.Application.Modules;
using BuildingBlocks.Application.UnitOfWork;
using BuildingBlocks.EventBus;
using BuildingBlocks.Infrastructure.Outbox;
using BuildingBlocks.Infrastructure.Persistence.Interceptors;
using BuildingBlocks.Infrastructure.UnitOfWork;
using BuildingBlocks.UnitOfWork;
using Customers.Application;
using Customers.Application.Customers.Queries;
using Customers.Contracts;
using Customers.Domain.Customers;
using Customers.Infrastructure.Persistence;
using Customers.Infrastructure.Persistence.Repositories;
using Customers.Infrastructure.PublicApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Customers.Infrastructure;

/// <summary>
/// Wires everything the Customers module needs into the shared DI container.
/// The host bootstrapper calls <see cref="AddServices"/> — it doesn't know
/// anything about the module's internals.
/// </summary>
public sealed class CustomersModule : IModule
{
    public string Name => "Customers";
    public string DatabaseSchema => CustomersDbContext.SchemaName;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing connection string 'Postgres'.");

        services.AddScoped<OutboxAccumulator>();
        services.AddScoped<IIntegrationEventQueue>(sp => sp.GetRequiredService<OutboxAccumulator>());

        services.AddDbContext<CustomersDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npg =>
            {
                npg.MigrationsHistoryTable("__ef_migrations_history", CustomersDbContext.SchemaName);
                npg.MigrationsAssembly(typeof(CustomersDbContext).Assembly.FullName);
                npg.EnableRetryOnFailure(3);
            });
            options.UseSnakeCaseNamingConvention();
            options.AddInterceptors(
                sp.GetRequiredService<AuditingInterceptor>(),
                sp.GetRequiredService<SoftDeleteInterceptor>(),
                sp.GetRequiredService<OutboxInterceptor>());
        });

        services.TryAddSingleton<AuditingInterceptor>();
        services.TryAddSingleton<SoftDeleteInterceptor>();
        services.AddScoped<OutboxInterceptor>();

        // UoW factory bound to this module's DbContext
        services.AddScoped<IUnitOfWorkFactory, EfCoreUnitOfWorkFactory<CustomersDbContext>>();

        // Repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerReadModel, CustomerReadModel>();

        // Public API surface for other modules
        services.AddScoped<ICustomersApi, CustomersApi>();

        // CQRS pipeline scoped to THIS module's assemblies
        services.AddApplicationPipeline(
            typeof(CustomerPermissions).Assembly,
            typeof(CustomersModule).Assembly);

        // Outbox drain
        services.AddHostedService<BuildingBlocks.Infrastructure.Outbox.OutboxProcessor<CustomersDbContext>>();
    }
}
