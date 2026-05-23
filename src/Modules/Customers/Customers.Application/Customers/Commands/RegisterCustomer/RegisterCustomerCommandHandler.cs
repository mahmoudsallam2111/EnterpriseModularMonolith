using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.EventBus;
using BuildingBlocks.SharedKernel;
using Customers.Domain.Customers;
using Customers.IntegrationEvents;

namespace Customers.Application.Customers.Commands.RegisterCustomer;

internal sealed class RegisterCustomerCommandHandler : ICommandHandler<RegisterCustomerCommand, Guid>
{
    private readonly ICustomerRepository _repository;
    private readonly IIntegrationEventQueue _integrationEventQueue;
    private readonly IClock _clock;

    public RegisterCustomerCommandHandler(
        ICustomerRepository repository,
        IIntegrationEventQueue integrationEventQueue,
        IClock clock)
    {
        _repository = repository;
        _integrationEventQueue = integrationEventQueue;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(RegisterCustomerCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);

        if (await _repository.ExistsByEmailAsync(email, cancellationToken))
            return Error.Conflict("Customers.EmailTaken", $"Email '{email}' is already registered.");

        var name = PersonName.Create(request.FirstName, request.LastName);
        var customer = Customer.Register(name, email);

        await _repository.AddAsync(customer, cancellationToken);

        _integrationEventQueue.Enqueue(new CustomerRegisteredIntegrationEvent(
            customer.Id.Value, name.Full, email.Value, _clock.UtcNow));

        return customer.Id.Value;
    }
}
