using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.EventBus;
using BuildingBlocks.SharedKernel;
using Customers.Contracts;
using FluentValidation;
using Orders.Domain.Orders;
using Orders.IntegrationEvents;

namespace Orders.Application.Orders.Commands.PlaceOrder;

[RequiresPermission(OrderPermissions.Manage)]
public sealed record PlaceOrderCommand(
    Guid CustomerId,
    string Currency,
    IReadOnlyList<PlaceOrderLine> Lines) : ICommand<Guid>;

public sealed record PlaceOrderLine(string Sku, string Name, int Quantity, decimal UnitPrice);

public sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Sku).NotEmpty().MaximumLength(64);
            line.RuleFor(l => l.Name).NotEmpty().MaximumLength(200);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThan(0);
        });
    }
}

internal sealed class PlaceOrderCommandHandler : ICommandHandler<PlaceOrderCommand, Guid>
{
    private readonly IOrderRepository _repository;
    private readonly ICustomersApi _customersApi;
    private readonly IIntegrationEventQueue _integrationEventQueue;
    private readonly IClock _clock;

    public PlaceOrderCommandHandler(
        IOrderRepository repository,
        ICustomersApi customersApi,
        IIntegrationEventQueue integrationEventQueue,
        IClock clock)
    {
        _repository = repository;
        _customersApi = customersApi;
        _integrationEventQueue = integrationEventQueue;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        // Cross-module read: only via the public API contract.
        if (!await _customersApi.IsCustomerActiveAsync(request.CustomerId, cancellationToken))
            return Error.Validation("Orders.CustomerNotActive",
                $"Customer {request.CustomerId} is not active or does not exist.");

        var order = Order.Draft(request.CustomerId, request.Currency);
        foreach (var line in request.Lines)
            order.AddLine(line.Sku, line.Name, line.Quantity, line.UnitPrice);

        order.Place(_clock.UtcNow);
        await _repository.AddAsync(order, cancellationToken);

        _integrationEventQueue.Enqueue(new OrderPlacedIntegrationEvent(
            order.Id.Value, order.CustomerId,
            order.Total.Amount, order.Total.Currency,
            order.Lines.Count, _clock.UtcNow));

        return order.Id.Value;
    }
}
