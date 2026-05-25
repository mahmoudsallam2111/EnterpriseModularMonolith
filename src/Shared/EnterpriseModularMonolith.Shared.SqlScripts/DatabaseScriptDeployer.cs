using System.Reflection;
using DbUp;
using DbUp.Engine;
using DbUp.Helpers;
using Microsoft.Extensions.Logging;

namespace EnterpriseModularMonolith.Shared.SqlScripts;

public sealed class DatabaseScriptDeployer : IDatabaseScriptDeployer
{
    private const string OneTimeScriptsMarker = ".Onetime.";
    private const string EveryTimeScriptsMarker = ".Everytime.";

    private readonly ILogger<DatabaseScriptDeployer> _logger;

    public DatabaseScriptDeployer(ILogger<DatabaseScriptDeployer> logger)
    {
        _logger = logger;
    }

    public Task DeployAsync(
        DatabaseScriptDeployOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new InvalidOperationException("A PostgreSQL connection string is required to deploy SQL scripts.");

        if (options.RunOneTimeScripts)
        {
            var oneTime = BuildOneTimeUpgradeEngine(options.ConnectionString);
            RunUpgrade("one-time", oneTime, options.DryRun);
        }

        if (options.RunEveryTimeScripts)
        {
            var everyTime = BuildEveryTimeUpgradeEngine(options.ConnectionString);
            RunUpgrade("every-time", everyTime, options.DryRun);
        }

        return Task.CompletedTask;
    }

    private static UpgradeEngine BuildOneTimeUpgradeEngine(string connectionString)
    {
        var builder = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                resourceName => resourceName.Contains(OneTimeScriptsMarker, StringComparison.OrdinalIgnoreCase))
            .LogToConsole();

        return builder.WithTransactionPerScript().Build();
    }

    private static UpgradeEngine BuildEveryTimeUpgradeEngine(string connectionString)
    {
        var builder = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                resourceName => resourceName.Contains(EveryTimeScriptsMarker, StringComparison.OrdinalIgnoreCase))
            .JournalTo(new NullJournal())
            .LogToConsole();

        return builder.WithTransactionPerScript().Build();
    }

    private void RunUpgrade(string bucket, UpgradeEngine upgradeEngine, bool dryRun)
    {
        var scripts = upgradeEngine.GetScriptsToExecute().ToArray();
        _logger.LogInformation(
            "DbUp {Bucket} scripts to {Action}: {Count}",
            bucket,
            dryRun ? "preview" : "execute",
            scripts.Length);

        foreach (var script in scripts)
        {
            _logger.LogInformation("DbUp pending {Bucket} script: {ScriptName}", bucket, script.Name);
        }

        if (dryRun)
            return;

        var result = upgradeEngine.PerformUpgrade();
        if (!result.Successful)
        {
            _logger.LogError(result.Error, "DbUp {Bucket} script deployment failed", bucket);
            throw result.Error;
        }

        _logger.LogInformation("DbUp {Bucket} script deployment completed successfully", bucket);
    }
}
