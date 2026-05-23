using BuildingBlocks.Presentation.Endpoints;
using Customers.Application.Customers.Commands.ChangeEmail;
using Customers.Application.Customers.Commands.DeactivateCustomer;
using Customers.Application.Customers.Commands.RegisterCustomer;
using Customers.Application.Customers.Queries.GetCustomer;
using Customers.Application.Customers.Queries.ListCustomers;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Customers.Presentation;

/// <summary>
/// Minimal API endpoints for the Customers module. Mounted by the host at /api/v1/customers.
/// Endpoints are thin — they translate HTTP &lt;-&gt; commands/queries and rely on the MediatR
/// pipeline for validation, authorization, tracing, and the unit of work.
/// </summary>
public sealed class CustomerEndpoints : IModuleEndpoints
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/customers")
            .WithTags("Customers")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapPost("/", RegisterAsync)
            .WithName("RegisterCustomer")
            .WithSummary("Register a new customer.");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetCustomerById")
            .WithSummary("Get a customer by id.");

        group.MapGet("/", ListAsync)
            .WithName("ListCustomers")
            .WithSummary("List customers (paged, optional search/status filter).");

        group.MapPatch("/{id:guid}/email", ChangeEmailAsync)
            .WithName("ChangeCustomerEmail")
            .WithSummary("Change a customer's email.");

        group.MapPost("/{id:guid}/deactivate", DeactivateAsync)
            .WithName("DeactivateCustomer")
            .WithSummary("Deactivate a customer.");
    }

    private static async Task<IResult> RegisterAsync(
        RegisterCustomerCommand command, ISender mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(command, cancellationToken))
            .ToHttpResult(id => Results.Created($"/api/v1/customers/{id}", new { id }));

    private static async Task<IResult> GetByIdAsync(
        Guid id, ISender mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new GetCustomerByIdQuery(id), cancellationToken)).ToHttpResult();

    private static async Task<IResult> ListAsync(
        ISender mediator,
        CancellationToken cancellationToken,
        string? search = null, string? status = null, int page = 1, int pageSize = 20) =>
        (await mediator.Send(new ListCustomersQuery(search, status, page, pageSize), cancellationToken))
            .ToHttpResult();

    private static async Task<IResult> ChangeEmailAsync(
        Guid id, ChangeEmailBody body, ISender mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new ChangeEmailCommand(id, body.NewEmail), cancellationToken)).ToHttpResult();

    private static async Task<IResult> DeactivateAsync(
        Guid id, DeactivateBody body, ISender mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new DeactivateCustomerCommand(id, body.Reason), cancellationToken)).ToHttpResult();
}

public sealed record ChangeEmailBody(string NewEmail);
public sealed record DeactivateBody(string Reason);
