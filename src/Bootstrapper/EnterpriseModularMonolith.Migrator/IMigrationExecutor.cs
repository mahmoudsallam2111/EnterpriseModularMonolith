namespace EnterpriseModularMonolith.Migrator;

public interface IMigrationExecutor
{
    Task RunAsync(CancellationToken cancellationToken = default);
}
