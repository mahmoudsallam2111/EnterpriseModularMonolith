using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.EventBus;
using Customers.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Orders.Application.Orders.Commands.CancelOrder;
using Orders.Application.Orders.Queries;

namespace Orders.Application.Orders.EventHandlers;

/// <summary>
/// Reacts to a CustomerDeactivated integration event from the Customers module.
/// Strict module boundary: we only receive a flat DTO, never the Customer aggregate.
/// </summary>
public sealed class CustomerDeactivatedHandler : IIntegrationEventHandler<CustomerDeactivatedIntegrationEvent>
{
    private readonly ISender _mediator;
    private readonly IOrderLookupForCustomer _lookup;
    private readonly ILogger<CustomerDeactivatedHandler> _logger;

    public CustomerDeactivatedHandler(
        ISender mediator,
        IOrderLookupForCustomer lookup,
        ILogger<CustomerDeactivatedHandler> logger)
    {
        _mediator = mediator;
        _lookup = lookup;
        _logger = logger;
    }

    public async Task HandleAsync(CustomerDeactivatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var openOrders = await _lookup.GetOpenOrdersForCustomerAsync(integrationEvent.CustomerId, cancellationToken);
        _logger.LogInformation(
            "Customer {CustomerId} deactivated; cancelling {Count} open order(s).",
            integrationEvent.CustomerId, openOrders.Count);

        foreach (var orderId in openOrders)
        {
            var result = await _mediator.Send(
                new CancelOrderCommand(orderId, $"Customer deactivated: {integrationEvent.Reason}"),
                cancellationToken);
            if (result.IsFailure)
                _logger.LogWarning("Failed to cancel order {OrderId}: {Error}", orderId, result.Error);
        }
    }
}

/// <summary>
/// Small read-side helper for the cross-module event handler.
/// Implementation lives in Orders.Infrastructure.
/// </summary>
public interface IOrderLookupForCustomer
{
    Task<IReadOnlyList<Guid>> GetOpenOrdersForCustomerAsync(Guid customerId, CancellationToken cancellationToken);
}
