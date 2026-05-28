using BuildingBlocks.Application.Persistence;

namespace EmmModule.Domain.EmmModules;

/// <summary>
/// Domain-defined repository contract. Implementation lives in EmmModule.Infrastructure.
/// </summary>
public interface IEmmModuleSampleRepository : IRepository<EmmModuleSample, EmmModuleSampleId>;
