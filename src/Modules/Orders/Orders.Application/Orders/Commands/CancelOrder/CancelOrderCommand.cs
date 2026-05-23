using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.EventBus;
using BuildingBlocks.SharedKernel;
using FluentValidation;
using Orders.Domain.Orders;
using Orders.IntegrationEvents;

namespace Orders.Application.Orders.Commands.CancelOrder;

[RequiresPermission(OrderPermissions.Manage)]
public sealed record CancelOrderCommand(Guid OrderId, string Reason) : ICommand;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

internal sealed class CancelOrderCommandHandler : ICommandHandler<CancelOrderCommand>
{
    private readonly IOrderRepository _repository;
    private readonly IIntegrationEventQueue _integrationEventQueue;
    private readonly IClock _clock;

    public CancelOrderCommandHandler(IOrderRepository repository, IIntegrationEventQueue integrationEventQueue, IClock clock)
    {
        _repository = repository;
        _integrationEventQueue = integrationEventQueue;
        _clock = clock;
    }

    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetWithLinesAsync(OrderId.From(request.OrderId), cancellationToken);
        if (order is null)
            return Error.NotFound("Orders.NotFound", $"Order {request.OrderId} not found.");

        order.Cancel(request.Reason, _clock.UtcNow);
        _repository.Update(order);

        _integrationEventQueue.Enqueue(new OrderCancelledIntegrationEvent(
            order.Id.Value, order.CustomerId, request.Reason, _clock.UtcNow));

        return Result.Success();
    }
}
