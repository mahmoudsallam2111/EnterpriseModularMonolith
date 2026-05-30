using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.EventBus;
using Customers.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Orders.Application.Orders.Commands.CancelOrder;

namespace Orders.Application.Orders.EventHandlers;

/// <summary>
/// Reacts to a CustomerDeactivated integration event from the Customers module.
/// Strict module boundary: we only receive a flat DTO, never the Customer aggregate.
/// </summary>
public sealed class CustomerDeactivatedHandler : IIntegrationEventHandler<CustomerDeactivatedIntegrationEvent>
{
    private readonly ISender _mediator;
    private readonly IOrderQuery _query;
    private readonly ILogger<CustomerDeactivatedHandler> _logger;

    public CustomerDeactivatedHandler(
        ISender mediator,
        IOrderQuery query,
        ILogger<CustomerDeactivatedHandler> logger)
    {
        _mediator = mediator;
        _query = query;
        _logger = logger;
    }

    public async Task HandleAsync(CustomerDeactivatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var openOrders = await _query.GetOpenOrderIdsForCustomerAsync(integrationEvent.CustomerId, cancellationToken);
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
