using BuildingBlocks.Application;
using BuildingBlocks.Application.Modules;
using BuildingBlocks.Application.UnitOfWork;
using BuildingBlocks.Auditing.Interceptors;
using BuildingBlocks.EventBus;
using BuildingBlocks.Infrastructure.Outbox;
using BuildingBlocks.Infrastructure.Persistence.Interceptors;
using BuildingBlocks.Infrastructure.UnitOfWork;
using EmmModule.Application;
using EmmModule.Application.EmmModules.Queries.GetSample;
using EmmModule.Contracts;
using EmmModule.Domain.EmmModules;
using EmmModule.Infrastructure.Persistence;
using EmmModule.Infrastructure.Persistence.Repositories;
using EmmModule.Infrastructure.PublicApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace EmmModule.Infrastructure;

/// <summary>
/// Wires everything the EmmModule module needs into the shared DI container.
/// Add ONE line in the host's ModuleRegistry: <c>new EmmModuleModule()</c>.
/// </summary>
public sealed class EmmModuleModule : IModule
{
    public string Name => "EmmModule";
    public string DatabaseSchema => EmmModuleDbContext.SchemaName;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<OutboxAccumulator>();
        services.AddScoped<IIntegrationEventQueue>(sp => sp.GetRequiredService<OutboxAccumulator>());

        services.AddDbContext<EmmModuleDbContext>((sp, options) =>
        {
            var connection = sp.GetRequiredService<SharedModuleDbConnection>().Connection;
            options.UseNpgsql(connection, npg =>
            {
                npg.MigrationsHistoryTable("__ef_migrations_history", EmmModuleDbContext.SchemaName);
                npg.MigrationsAssembly(typeof(EmmModuleDbContext).Assembly.FullName);
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

        services.AddScoped<IUnitOfWorkCommitter>(sp => sp.GetRequiredService<EmmModuleDbContext>());

        services.AddScoped<IEmmModuleSampleRepository, EmmModuleSampleRepository>();
        services.AddScoped<IEmmModuleReadModel, EmmModuleReadModel>();
        services.AddScoped<IEmmModuleApi, EmmModuleApi>();

        services.AddApplicationPipeline(
            typeof(EmmModulePermissions).Assembly,
            typeof(EmmModuleModule).Assembly);

        services.AddHostedService<OutboxProcessor<EmmModuleDbContext>>();
    }
}
