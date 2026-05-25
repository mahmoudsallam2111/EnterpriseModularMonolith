using BuildingBlocks.Application;
using BuildingBlocks.Application.Modules;
using BuildingBlocks.Application.UnitOfWork;
using BuildingBlocks.Auditing.Interceptors;
using BuildingBlocks.EventBus;
using BuildingBlocks.Infrastructure.Outbox;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Interceptors;
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
        services.AddScoped<OutboxAccumulator>();
        services.AddScoped<IIntegrationEventQueue>(sp => sp.GetRequiredService<OutboxAccumulator>());

        services.AddDbContext<CustomersDbContext>((sp, options) =>
        {
            var connection = sp.GetRequiredService<SharedModuleDbConnection>().Connection;

            options.UseNpgsql(connection, npg =>
            {
                npg.MigrationsHistoryTable("__ef_migrations_history", CustomersDbContext.SchemaName);
                npg.MigrationsAssembly(typeof(CustomersDbContext).Assembly.FullName);
            });
            options.UseSnakeCaseNamingConvention();
            options.AddInterceptors(
                sp.GetRequiredService<AuditingInterceptor>(),
                sp.GetRequiredService<SoftDeleteInterceptor>(),
                sp.GetRequiredService<OutboxInterceptor>(),
                sp.GetRequiredService<AuditCapturingInterceptor>());
        });

        services.TryAddScoped<AuditingInterceptor>();
        services.TryAddScoped<SoftDeleteInterceptor>();
        services.AddScoped<OutboxInterceptor>();

        // Register this module's DbContext as a committer for the UoW pipeline behavior
        services.AddScoped<IUnitOfWorkCommitter>(sp => sp.GetRequiredService<CustomersDbContext>());

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
        services.AddHostedService<OutboxProcessor<CustomersDbContext>>();
    }
}
