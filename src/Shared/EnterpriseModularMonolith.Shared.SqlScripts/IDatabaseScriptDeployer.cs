namespace EnterpriseModularMonolith.Shared.SqlScripts;

public interface IDatabaseScriptDeployer
{
    Task DeployAsync(DatabaseScriptDeployOptions options, CancellationToken cancellationToken = default);
}
