using EmmModule.Application.EmmModules.Queries.GetSample;
using EmmModule.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EmmModule.Infrastructure.Persistence.Repositories;

internal sealed class EmmModuleReadModel : IEmmModuleReadModel
{
    private readonly EmmModuleDbContext _context;
    public EmmModuleReadModel(EmmModuleDbContext context) => _context = context;

    public Task<EmmModuleSummaryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Samples
            .AsNoTracking()
            .Where(s => s.Id.Value == id)
            .Select(s => new EmmModuleSummaryDto(s.Id.Value, s.Name, s.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
}
