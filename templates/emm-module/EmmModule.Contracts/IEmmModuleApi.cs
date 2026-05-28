namespace EmmModule.Contracts;

/// <summary>
/// Public surface of the EmmModule module. Other modules reference ONLY this
/// contract when they need data from EmmModule — never the domain types or DbContext.
/// </summary>
public interface IEmmModuleApi
{
    Task<EmmModuleSummaryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record EmmModuleSummaryDto(Guid Id, string Name, DateTimeOffset CreatedAtUtc);
