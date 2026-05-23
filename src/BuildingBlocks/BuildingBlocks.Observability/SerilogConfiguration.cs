using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace BuildingBlocks.Observability;

public static class SerilogConfiguration
{
    /// <summary>
    /// Configures Serilog as the application logger. Reads sinks/levels from
    /// configuration (appsettings.json -> "Serilog" section) and enriches every
    /// log event with process/thread/environment data plus the correlation ID.
    /// </summary>
    public static IHostBuilder UseEnterpriseSerilog(this IHostBuilder host) =>
        host.UseSerilog((context, services, logger) =>
        {
            logger
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithEnvironmentUserName()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName);
        });
}
