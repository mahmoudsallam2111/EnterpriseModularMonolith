using EnterpriseModularMonolith.Migrator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .ConfigureServices((context, services) =>
    {
        services.AddMigrator(context.Configuration);
    })
    .Build();

await using var scope = host.Services.CreateAsyncScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

try
{
    var executor = scope.ServiceProvider.GetRequiredService<IMigrationExecutor>();
    await executor.RunAsync();
    Environment.ExitCode = 0;
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Database migrator failed");
    Environment.ExitCode = 1;
}
