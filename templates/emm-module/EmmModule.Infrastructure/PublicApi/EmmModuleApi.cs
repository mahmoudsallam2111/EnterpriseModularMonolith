using EmmModule.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EmmModule.Infrastructure.PublicApi;

/// <summary>
/// Default implementation of the module's public API surface. Other modules
/// consume this through <see cref="IEmmModuleApi"/> — they never see Domain types.
/// </summary>
internal sealed class EmmModuleApi : IEmmModuleApi
{
    private readonly Persistence.EmmModuleDbContext _context;
    public EmmModuleApi(Persistence.EmmModuleDbContext context) => _context = context;

    public Task<EmmModuleSummaryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Samples
            .AsNoTracking()
            .Where(s => s.Id.Value == id)
            .Select(s => new EmmModuleSummaryDto(s.Id.Value, s.Name, s.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Samples.AsNoTracking().AnyAsync(s => s.Id.Value == id, cancellationToken);
}
