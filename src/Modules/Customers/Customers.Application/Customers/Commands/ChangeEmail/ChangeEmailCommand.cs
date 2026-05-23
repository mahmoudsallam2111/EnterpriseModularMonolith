using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.Domain;
using BuildingBlocks.SharedKernel;
using Customers.Domain.Customers;
using FluentValidation;

namespace Customers.Application.Customers.Commands.ChangeEmail;

[RequiresPermission(CustomerPermissions.Manage)]
public sealed record ChangeEmailCommand(Guid CustomerId, string NewEmail) : ICommand;

public sealed class ChangeEmailCommandValidator : AbstractValidator<ChangeEmailCommand>
{
    public ChangeEmailCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.NewEmail).NotEmpty().EmailAddress();
    }
}

internal sealed class ChangeEmailCommandHandler : ICommandHandler<ChangeEmailCommand>
{
    private readonly ICustomerRepository _repository;

    public ChangeEmailCommandHandler(ICustomerRepository repository) => _repository = repository;

    public async Task<Result> Handle(ChangeEmailCommand request, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(CustomerId.From(request.CustomerId), cancellationToken);
        if (customer is null)
            return Error.NotFound("Customers.NotFound", $"Customer {request.CustomerId} not found.");

        var newEmail = Email.Create(request.NewEmail);
        if (await _repository.ExistsByEmailAsync(newEmail, cancellationToken))
            return Error.Conflict("Customers.EmailTaken", $"Email '{newEmail}' is already registered.");

        customer.ChangeEmail(newEmail);
        _repository.Update(customer);
        return Result.Success();
    }
}
