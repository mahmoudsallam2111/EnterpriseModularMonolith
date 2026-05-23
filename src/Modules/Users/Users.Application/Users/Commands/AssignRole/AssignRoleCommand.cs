using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.SharedKernel;
using FluentValidation;
using Users.Domain.Users;

namespace Users.Application.Users.Commands.AssignRole;

[RequiresPermission(UserPermissions.ManageRoles)]
public sealed record AssignRoleCommand(Guid UserId, Guid RoleId) : ICommand;

public sealed class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleId).NotEmpty();
    }
}

internal sealed class AssignRoleCommandHandler : ICommandHandler<AssignRoleCommand>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;

    public AssignRoleCommandHandler(IUserRepository users, IRoleRepository roles)
    {
        _users = users;
        _roles = roles;
    }

    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(UserId.From(request.UserId), cancellationToken);
        if (user is null)
            return Error.NotFound("Users.NotFound", $"User {request.UserId} not found.");

        var role = await _roles.GetByIdAsync(RoleId.From(request.RoleId), cancellationToken);
        if (role is null)
            return Error.NotFound("Users.RoleNotFound", $"Role {request.RoleId} not found.");

        user.AssignRole(role.Id);
        _users.Update(user);
        return Result.Success();
    }
}
