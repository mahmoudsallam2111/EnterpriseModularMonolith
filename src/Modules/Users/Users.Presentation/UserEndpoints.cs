using BuildingBlocks.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Users.Application.Users.Commands.AssignRole;
using Users.Application.Users.Commands.Login;
using Users.Application.Users.Commands.RegisterUser;
using Users.Application.Users.Queries.GetMe;

namespace Users.Presentation;

public sealed class UserEndpoints : IModuleEndpoints
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        // Anonymous auth group
        var auth = endpoints.MapGroup("/api/v1/auth")
            .WithTags("Auth")
            .AllowAnonymous()
            .WithOpenApi();

        auth.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Authenticate with credentials and receive a JWT.");

        // Authenticated users group
        var users = endpoints.MapGroup("/api/v1/users")
            .WithTags("Users")
            .RequireAuthorization()
            .WithOpenApi();

        users.MapGet("/me", GetMeAsync)
            .WithName("GetMe")
            .WithSummary("Current authenticated user.");

        users.MapPost("/", RegisterAsync)
            .WithName("RegisterUser")
            .WithSummary("Register a new user.");

        users.MapPost("/{id:guid}/roles", AssignRoleAsync)
            .WithName("AssignRole")
            .WithSummary("Assign a role to a user.");
    }

    private static async Task<IResult> LoginAsync(
        LoginCommand command, ISender mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(command, cancellationToken)).ToHttpResult();

    private static async Task<IResult> GetMeAsync(
        ISender mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new GetMeQuery(), cancellationToken)).ToHttpResult();

    private static async Task<IResult> RegisterAsync(
        RegisterUserCommand command, ISender mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(command, cancellationToken))
            .ToHttpResult(id => Results.Created($"/api/v1/users/{id}", new { id }));

    private static async Task<IResult> AssignRoleAsync(
        Guid id, AssignRoleBody body, ISender mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new AssignRoleCommand(id, body.RoleId), cancellationToken)).ToHttpResult();
}

public sealed record AssignRoleBody(Guid RoleId);
