using BuildingBlocks.Infrastructure.Persistence.Repositories;
using EmmModule.Domain.EmmModules;

namespace EmmModule.Infrastructure.Persistence.Repositories;

internal sealed class EmmModuleSampleRepository
    : EfWriteRepository<EmmModuleDbContext, EmmModuleSample, EmmModuleSampleId>, IEmmModuleSampleRepository
{
    public EmmModuleSampleRepository(EmmModuleDbContext context) : base(context) { }
}
