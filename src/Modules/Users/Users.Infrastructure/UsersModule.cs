using BuildingBlocks.Application;
using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Application.Modules;
using BuildingBlocks.EventBus;
using BuildingBlocks.Infrastructure.Outbox;
using BuildingBlocks.Infrastructure.Persistence.Interceptors;
using BuildingBlocks.Infrastructure.UnitOfWork;
using BuildingBlocks.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Users.Application;
using Users.Application.Auth;
using Users.Application.Users.Queries.GetMe;
using Users.Contracts;
using Users.Domain.Users;
using Users.Infrastructure.Persistence;
using Users.Infrastructure.Persistence.Repositories;
using Users.Infrastructure.PublicApi;
using Users.Infrastructure.Security;

namespace Users.Infrastructure;

public sealed class UsersModule : IModule
{
    public string Name => "Users";
    public string DatabaseSchema => UsersDbContext.SchemaName;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing connection string 'Postgres'.");

        services.AddScoped<OutboxAccumulator>();
        services.AddScoped<IIntegrationEventQueue>(sp => sp.GetRequiredService<OutboxAccumulator>());

        services.AddDbContext<UsersDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npg =>
            {
                npg.MigrationsHistoryTable("__ef_migrations_history", UsersDbContext.SchemaName);
                npg.MigrationsAssembly(typeof(UsersDbContext).Assembly.FullName);
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

        services.AddScoped<IUnitOfWorkFactory, EfCoreUnitOfWorkFactory<UsersDbContext>>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserReadModel, UserReadModel>();

        // Security
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<ITokenIssuer, JwtTokenIssuer>();

        // The Users module is the permission authority for the whole solution.
        services.AddScoped<IPermissionService, PermissionService>();

        // Public API
        services.AddScoped<IUsersApi, UsersApi>();

        services.AddApplicationPipeline(
            typeof(UserPermissions).Assembly,
            typeof(UsersModule).Assembly);

        services.AddHostedService<BuildingBlocks.Infrastructure.Outbox.OutboxProcessor<UsersDbContext>>();
    }
}
