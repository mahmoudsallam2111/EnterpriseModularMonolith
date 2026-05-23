using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.EventBus;
using BuildingBlocks.SharedKernel;
using FluentValidation;
using Users.Domain.Users;
using Users.IntegrationEvents;

namespace Users.Application.Users.Commands.RegisterUser;

[RequiresPermission(UserPermissions.Manage)]
public sealed record RegisterUserCommand(string UserName, string Email, string Password) : ICommand<Guid>;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

internal sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, Guid>
{
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _hasher;
    private readonly IIntegrationEventQueue _integrationEventQueue;
    private readonly IClock _clock;

    public RegisterUserCommandHandler(
        IUserRepository repository,
        IPasswordHasher hasher,
        IIntegrationEventQueue integrationEventQueue,
        IClock clock)
    {
        _repository = repository;
        _hasher = hasher;
        _integrationEventQueue = integrationEventQueue;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await _repository.UserNameTakenAsync(request.UserName, cancellationToken))
            return Error.Conflict("Users.UserNameTaken", $"Username '{request.UserName}' is already taken.");

        var email = UserEmail.Create(request.Email);
        if (await _repository.EmailTakenAsync(email, cancellationToken))
            return Error.Conflict("Users.EmailTaken", $"Email '{email}' is already registered.");

        var password = _hasher.Hash(request.Password);
        var user = User.Register(request.UserName, email, password);

        await _repository.AddAsync(user, cancellationToken);

        _integrationEventQueue.Enqueue(new UserRegisteredIntegrationEvent(
            user.Id.Value, request.UserName, email.Value, _clock.UtcNow));

        return user.Id.Value;
    }
}
