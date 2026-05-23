using BuildingBlocks.Presentation.Endpoints;
using Customers.Presentation;
using Orders.Presentation;
using Microsoft.AspNetCore.Routing;
using Users.Presentation;

namespace EnterpriseModularMonolith.Api.Composition;

internal static class EndpointsBootstrap
{
    private static readonly IModuleEndpoints[] AllEndpoints =
    {
        new UserEndpoints(),
        new CustomerEndpoints(),
        new OrderEndpoints(),
    };

    public static IEndpointRouteBuilder MapModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        foreach (var module in AllEndpoints)
            module.Map(endpoints);
        return endpoints;
    }
}
