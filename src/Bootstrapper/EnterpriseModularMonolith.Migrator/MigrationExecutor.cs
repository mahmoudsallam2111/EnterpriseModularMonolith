using BuildingBlocks.Infrastructure.Seeding;
using EnterpriseModularMonolith.Shared.SqlScripts;
using Customers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orders.Infrastructure.Persistence;

namespace EnterpriseModularMonolith.Migrator;

public sealed class MigrationExecutor : IMigrationExecutor
{
    private readonly IConfiguration _configuration;
    private readonly MigratorOptions _options;
    private readonly CustomersDbContext _customersDbContext;
    private readonly OrdersDbContext _ordersDbContext;
    private readonly IEnumerable<IDataSeeder> _seeders;
    private readonly IDatabaseScriptDeployer _scriptDeployer;
    private readonly ILogger<MigrationExecutor> _logger;

    public MigrationExecutor(
        IConfiguration configuration,
        IOptions<MigratorOptions> options,
        CustomersDbContext customersDbContext,
        OrdersDbContext ordersDbContext,
        IEnumerable<IDataSeeder> seeders,
        IDatabaseScriptDeployer scriptDeployer,
        ILogger<MigrationExecutor> logger)
    {
        _configuration = configuration;
        _options = options.Value;
        _customersDbContext = customersDbContext;
        _ordersDbContext = ordersDbContext;
        _seeders = seeders;
        _scriptDeployer = scriptDeployer;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing connection string 'Postgres'.");

        _logger.LogInformation("Starting Enterprise Modular Monolith database migrator");

        if (_options.RunEfMigrations)
        {
            await MigrateAsync("Customers", _customersDbContext, cancellationToken);
            await MigrateAsync("Orders", _ordersDbContext, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Skipping EF Core migrations because Migrator:RunEfMigrations is false");
        }

        if (_options.RunSeeders)
        {
            foreach (var seeder in _seeders.OrderBy(seeder => seeder.Order))
            {
                _logger.LogInformation("Running data seeder {Seeder}", seeder.GetType().Name);
                await seeder.SeedAsync(cancellationToken);
            }
        }
        else
        {
            _logger.LogInformation("Skipping data seeders because Migrator:RunSeeders is false");
        }

        if (_options.RunSqlScripts)
        {
            await _scriptDeployer.DeployAsync(
                new DatabaseScriptDeployOptions
                {
                    ConnectionString = connectionString,
                    RunOneTimeScripts = _options.RunOneTimeScripts,
                    RunEveryTimeScripts = _options.RunEveryTimeScripts,
                    DryRun = _options.DryRun
                },
                cancellationToken);
        }
        else
        {
            _logger.LogInformation("Skipping DbUp SQL scripts because Migrator:RunSqlScripts is false");
        }

        _logger.LogInformation("Database migrator completed successfully");
    }

    private async Task MigrateAsync(
        string moduleName,
        DbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (_options.DryRun)
        {
            var pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            _logger.LogInformation(
                "EF Core dry run for {Module}: {Count} pending migration(s): {Migrations}",
                moduleName,
                pending.Count(),
                string.Join(", ", pending));
            return;
        }

        _logger.LogInformation("Applying EF Core migrations for {Module}", moduleName);
        await dbContext.Database.MigrateAsync(cancellationToken);
        _logger.LogInformation("EF Core migrations for {Module} completed", moduleName);
    }
}
