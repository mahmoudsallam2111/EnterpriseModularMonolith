using BuildingBlocks.Presentation.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Inventories.Presentation;

public sealed class InventoryEndpoints : IModuleEndpoints
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/inventories").WithTags("Inventories");
        
        group.MapGet("/", () => Results.Ok("Inventories module is alive"));
    }
}
