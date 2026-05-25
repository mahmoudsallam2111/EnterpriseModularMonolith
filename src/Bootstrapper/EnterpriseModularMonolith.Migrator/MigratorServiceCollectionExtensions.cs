using BuildingBlocks.Application.Security;
using BuildingBlocks.EventBus;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Interceptors;
using BuildingBlocks.Infrastructure.Seeding;
using BuildingBlocks.MultiTenancy;
using BuildingBlocks.SharedKernel;
using Customers.Infrastructure.Persistence;
using Customers.Infrastructure.Seeding;
using EnterpriseModularMonolith.Shared.SqlScripts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Infrastructure.Persistence;

namespace EnterpriseModularMonolith.Migrator;

public static class MigratorServiceCollectionExtensions
{
    public static IServiceCollection AddMigrator(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing connection string 'Postgres'.");

        services.Configure<MigratorOptions>(configuration.GetSection("Migrator"));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ITenantContext, NullTenantContext>();
        services.AddScoped<ICurrentUser, MigratorCurrentUser>();
        services.AddScoped<IDomainEventDispatcher, NoOpDomainEventDispatcher>();

        services.AddScoped<AuditingInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped(_ => new SharedModuleDbConnection(connectionString));

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
                sp.GetRequiredService<SoftDeleteInterceptor>());
        });

        services.AddDbContext<OrdersDbContext>((sp, options) =>
        {
            var connection = sp.GetRequiredService<SharedModuleDbConnection>().Connection;

            options.UseNpgsql(connection, npg =>
            {
                npg.MigrationsHistoryTable("__ef_migrations_history", OrdersDbContext.SchemaName);
                npg.MigrationsAssembly(typeof(OrdersDbContext).Assembly.FullName);
            });
            options.UseSnakeCaseNamingConvention();
            options.AddInterceptors(
                sp.GetRequiredService<AuditingInterceptor>(),
                sp.GetRequiredService<SoftDeleteInterceptor>());
        });

        services.AddScoped<IDataSeeder, CustomersSeeder>();
        services.AddScoped<IDatabaseScriptDeployer, DatabaseScriptDeployer>();
        services.AddScoped<IMigrationExecutor, MigrationExecutor>();

        return services;
    }
}
