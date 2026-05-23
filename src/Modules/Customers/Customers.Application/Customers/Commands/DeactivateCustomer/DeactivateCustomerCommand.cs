using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.EventBus;
using BuildingBlocks.SharedKernel;
using Customers.Domain.Customers;
using Customers.IntegrationEvents;
using FluentValidation;

namespace Customers.Application.Customers.Commands.DeactivateCustomer;

[RequiresPermission(CustomerPermissions.Manage)]
public sealed record DeactivateCustomerCommand(Guid CustomerId, string Reason) : ICommand;

public sealed class DeactivateCustomerCommandValidator : AbstractValidator<DeactivateCustomerCommand>
{
    public DeactivateCustomerCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

internal sealed class DeactivateCustomerCommandHandler : ICommandHandler<DeactivateCustomerCommand>
{
    private readonly ICustomerRepository _repository;
    private readonly IIntegrationEventQueue _integrationEventQueue;
    private readonly IClock _clock;

    public DeactivateCustomerCommandHandler(
        ICustomerRepository repository,
        IIntegrationEventQueue integrationEventQueue,
        IClock clock)
    {
        _repository = repository;
        _integrationEventQueue = integrationEventQueue;
        _clock = clock;
    }

    public async Task<Result> Handle(DeactivateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(CustomerId.From(request.CustomerId), cancellationToken);
        if (customer is null)
            return Error.NotFound("Customers.NotFound", $"Customer {request.CustomerId} not found.");

        customer.Deactivate(request.Reason);
        _repository.Update(customer);

        _integrationEventQueue.Enqueue(new CustomerDeactivatedIntegrationEvent(
            request.CustomerId, request.Reason, _clock.UtcNow));

        return Result.Success();
    }
}
