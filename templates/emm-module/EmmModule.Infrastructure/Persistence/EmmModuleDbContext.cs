using BuildingBlocks.Application.DataFiltering;
using BuildingBlocks.EventBus;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.MultiTenancy;
using EmmModule.Domain.EmmModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmmModule.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the EmmModule module. Lives in its own "emmmodule" schema.
/// Global query filters for ISoftDeletable / IMultiTenantEntity are applied
/// automatically by the base <see cref="ModuleDbContext"/>.
/// </summary>
public sealed class EmmModuleDbContext : ModuleDbContext
{
    public const string SchemaName = "emmmodule";
    public override string Schema => SchemaName;

    public DbSet<EmmModuleSample> Samples => Set<EmmModuleSample>();

    public EmmModuleDbContext(
        DbContextOptions<EmmModuleDbContext> options,
        IDomainEventDispatcher domainEventDispatcher,
        IDataFilter dataFilter,
        ITenantContext tenantContext,
        ILogger<EmmModuleDbContext> logger)
        : base(options, domainEventDispatcher, dataFilter, tenantContext, logger) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmmModuleDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
