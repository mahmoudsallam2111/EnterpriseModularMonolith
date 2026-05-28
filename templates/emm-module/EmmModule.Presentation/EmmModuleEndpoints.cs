using BuildingBlocks.Presentation.Endpoints;
using EmmModule.Application.EmmModules.Commands.CreateSample;
using EmmModule.Application.EmmModules.Queries.GetSample;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EmmModule.Presentation;

public sealed class EmmModuleEndpoints : IModuleEndpoints
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/emmmodule")
            .WithTags("EmmModule")
            .RequireAuthorization();

        group.MapPost("/samples", CreateAsync)
            .WithName("CreateEmmModuleSample")
            .WithSummary("Create a sample EmmModule entity.");

        group.MapGet("/samples/{id:guid}", GetAsync)
            .WithName("GetEmmModuleSampleById")
            .WithSummary("Get an EmmModule sample by id.");
    }

    private static async Task<IResult> CreateAsync(
        CreateEmmModuleSampleCommand command, ISender mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(command, cancellationToken))
            .ToHttpResult(id => Results.Created($"/api/v1/emmmodule/samples/{id}", new { id }));

    private static async Task<IResult> GetAsync(
        Guid id, ISender mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new GetEmmModuleSampleByIdQuery(id), cancellationToken)).ToHttpResult();
}
