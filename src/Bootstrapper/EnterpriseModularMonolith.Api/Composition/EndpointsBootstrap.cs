using BuildingBlocks.Presentation.Endpoints;
using Customers.Presentation;
using Orders.Presentation;
using Microsoft.AspNetCore.Routing;
using Inventories.Presentation;

namespace EnterpriseModularMonolith.Api.Composition;

internal static class EndpointsBootstrap
{
    private static readonly IModuleEndpoints[] AllEndpoints =
    {
        new CustomerEndpoints(),
        new OrderEndpoints(),
        new InventoryEndpoints(),
    };

    public static IEndpointRouteBuilder MapModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        foreach (var module in AllEndpoints)
            module.Map(endpoints);
        return endpoints;
    }
}
