using Microsoft.AspNetCore.Routing;

namespace BuildingBlocks.Presentation.Endpoints;

/// <summary>
/// Modules implement this to map their HTTP endpoints. The host calls it from
/// the bootstrapper without taking a project reference on the module's Presentation assembly.
/// </summary>
public interface IModuleEndpoints
{
    void Map(IEndpointRouteBuilder endpoints);
}
