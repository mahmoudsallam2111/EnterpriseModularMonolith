namespace Users.Contracts;

/// <summary>
/// Public surface of the Users module. Other modules reference ONLY this contract
/// when they need user data — never the User aggregate or DbContext.
/// </summary>
public interface IUsersApi
{
    Task<UserSummaryDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record UserSummaryDto(
    Guid Id,
    string UserName,
    string Email,
    bool IsActive,
    IReadOnlyCollection<string> Roles);
