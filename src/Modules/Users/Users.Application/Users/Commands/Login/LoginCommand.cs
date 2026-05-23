using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.SharedKernel;
using FluentValidation;
using Users.Application.Auth;
using Users.Domain.Users;

namespace Users.Application.Users.Commands.Login;

public sealed record LoginCommand(string UserNameOrEmail, string Password) : ICommand<AuthToken>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.UserNameOrEmail).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

internal sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthToken>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IClock _clock;

    public LoginCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        IPasswordHasher hasher,
        ITokenIssuer tokenIssuer,
        IClock clock)
    {
        _users = users;
        _roles = roles;
        _hasher = hasher;
        _tokenIssuer = tokenIssuer;
        _clock = clock;
    }

    public async Task<Result<AuthToken>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.FindByUserNameAsync(request.UserNameOrEmail, cancellationToken);
        if (user is null && request.UserNameOrEmail.Contains('@'))
            user = await _users.FindByEmailAsync(UserEmail.Create(request.UserNameOrEmail), cancellationToken);

        if (user is null)
            return Error.Unauthorized("Auth.InvalidCredentials", "Invalid credentials.");

        if (user.IsLockedOut)
            return Error.Forbidden("Auth.LockedOut", user.LockoutReason ?? "Account is locked.");

        if (!_hasher.Verify(request.Password, user.Password))
        {
            user.RecordFailedLogin(_clock.UtcNow);
            _users.Update(user);
            return Error.Unauthorized("Auth.InvalidCredentials", "Invalid credentials.");
        }

        user.RecordSuccessfulLogin(_clock.UtcNow);
        _users.Update(user);

        // Aggregate roles + permissions
        var roleNames = new List<string>();
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var roleId in user.RoleIds)
        {
            var role = await _roles.GetByIdAsync(roleId, cancellationToken);
            if (role is null) continue;
            roleNames.Add(role.Name);
            foreach (var p in role.Permissions) permissions.Add(p);
        }

        var token = _tokenIssuer.Issue(
            user.Id.Value, user.UserName, user.Email.Value,
            roleNames, permissions);

        return token;
    }
}
