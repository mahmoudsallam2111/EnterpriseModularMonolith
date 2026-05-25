namespace EnterpriseModularMonolith.Shared.SqlScripts;

public sealed class DatabaseScriptDeployOptions
{
    public string? ConnectionString { get; set; }

    public bool RunOneTimeScripts { get; set; } = true;

    public bool RunEveryTimeScripts { get; set; } = true;

    public bool DryRun { get; set; }
}
