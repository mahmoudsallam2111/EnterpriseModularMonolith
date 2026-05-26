using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BuildingBlocks.Auditing.Persistence;

public sealed class AuditDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=AuditDb;Username=emm;Password=emm";

    public AuditDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Audit") ??
            Environment.GetEnvironmentVariable("AUDIT_CONNECTION_STRING") ??
            DefaultConnectionString;

        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseNpgsql(connectionString, npg =>
            {
                npg.MigrationsHistoryTable("__ef_migrations_history", AuditDbContext.SchemaName);
                npg.MigrationsAssembly(typeof(AuditDbContext).Assembly.FullName);
            })
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AuditDbContext(options);
    }
}
