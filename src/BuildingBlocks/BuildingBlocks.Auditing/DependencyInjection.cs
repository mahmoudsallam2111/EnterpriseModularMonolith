using BuildingBlocks.Auditing.Abstractions;
using BuildingBlocks.Auditing.Behaviors;
using BuildingBlocks.Auditing.Interceptors;
using BuildingBlocks.Auditing.Jobs;
using BuildingBlocks.Auditing.Middleware;
using BuildingBlocks.Auditing.Persistence;
using BuildingBlocks.Auditing.Scope;
using BuildingBlocks.Auditing.Writers;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quartz;

namespace BuildingBlocks.Auditing;

public static class DependencyInjection
{
    /// <summary>
    /// Wires the audit subsystem into the host:
    ///  • binds <see cref="AuditingOptions"/> from the "Auditing" config section,
    ///  • registers the AuditDbContext on the "Audit" connection string,
    ///  • registers the ambient scope, the bounded-channel writer, and the background drain,
    ///  • registers the MediatR <c>AuditingBehavior</c>,
    ///  • registers the EF interceptor that any module DbContext can pick up,
    ///  • optionally schedules the retention job (requires Quartz already registered).
    /// </summary>
    public static IServiceCollection AddAuditing(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuditingOptions>()
            .Bind(configuration.GetSection(AuditingOptions.SectionName));

        var auditConnectionString = configuration.GetConnectionString("Audit")
            ?? throw new InvalidOperationException("Missing connection string 'Audit'.");

        services.AddDbContext<AuditDbContext>(options =>
        {
            options.UseNpgsql(auditConnectionString, npg =>
            {
                npg.MigrationsHistoryTable("__ef_migrations_history", AuditDbContext.SchemaName);
                npg.MigrationsAssembly(typeof(AuditDbContext).Assembly.FullName);
                npg.EnableRetryOnFailure(3);
            });
            options.UseSnakeCaseNamingConvention();
        });

        services.AddSingleton<IAuditScopeAccessor, AmbientAuditScopeAccessor>();
        services.AddSingleton<ChannelAuditWriter>();
        services.AddSingleton<IAuditWriter>(sp => sp.GetRequiredService<ChannelAuditWriter>());
        services.AddHostedService<AuditWriterHostedService>();

        services.AddScoped<AuditCapturingInterceptor>();

        // MediatR pipeline behavior — appended (outermost first in MediatR semantics depends on registration order).
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(AuditingBehavior<,>)));

        return services;
    }

    /// <summary>
    /// Adds the AuditScopeMiddleware to the HTTP pipeline. Call AFTER
    /// <c>UseAuthentication()</c> so ICurrentUser is populated.
    /// </summary>
    public static IApplicationBuilder UseAuditing(this IApplicationBuilder app) =>
        app.UseMiddleware<AuditScopeMiddleware>();

    /// <summary>
    /// Optional. Adds the retention job to an already-configured Quartz container.
    /// </summary>
    public static IServiceCollectionQuartzConfigurator AddAuditRetentionJob(
        this IServiceCollectionQuartzConfigurator quartz,
        string cron)
    {
        var key = new JobKey("audit-retention", "audit");
        quartz.AddJob<AuditRetentionJob>(j => j.WithIdentity(key).StoreDurably());
        quartz.AddTrigger(t => t
            .ForJob(key)
            .WithIdentity("audit-retention-trigger", "audit")
            .WithCronSchedule(cron));
        return quartz;
    }
}
