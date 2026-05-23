using BuildingBlocks.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Orders.Application.Orders.Commands.CancelOrder;
using Orders.Application.Orders.Commands.PlaceOrder;
using Orders.Application.Orders.Queries.GetOrderById;

namespace Orders.Presentation;

public sealed class OrderEndpoints : IModuleEndpoints
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/orders")
            .WithTags("Orders")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapPost("/", PlaceAsync)
            .WithName("PlaceOrder")
            .WithSummary("Place a new order for a customer.");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetOrderById");

        group.MapPost("/{id:guid}/cancel", CancelAsync)
            .WithName("CancelOrder");
    }

    private static async Task<IResult> PlaceAsync(
        PlaceOrderCommand command, ISender mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(command, cancellationToken))
            .ToHttpResult(id => Results.Created($"/api/v1/orders/{id}", new { id }));

    private static async Task<IResult> GetByIdAsync(
        Guid id, ISender mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new GetOrderByIdQuery(id), cancellationToken)).ToHttpResult();

    private static async Task<IResult> CancelAsync(
        Guid id, CancelBody body, ISender mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new CancelOrderCommand(id, body.Reason), cancellationToken)).ToHttpResult();
}

public sealed record CancelBody(string Reason);
