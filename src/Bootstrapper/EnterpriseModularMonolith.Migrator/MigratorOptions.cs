namespace EnterpriseModularMonolith.Migrator;

public sealed class MigratorOptions
{
    public bool RunEfMigrations { get; set; } = true;

    public bool RunSeeders { get; set; } = true;

    public bool RunSqlScripts { get; set; } = true;

    public bool RunOneTimeScripts { get; set; } = true;

    public bool RunEveryTimeScripts { get; set; } = true;

    public bool DryRun { get; set; }
}
