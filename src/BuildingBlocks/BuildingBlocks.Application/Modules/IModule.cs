using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Application.Modules;

/// <summary>
/// Contract every business module implements. The bootstrapper host discovers
/// modules at startup and gives each one the chance to register its services
/// and map its endpoints, without the host knowing anything about their internals.
/// </summary>
public interface IModule
{
    string Name { get; }

    /// <summary>Default DB schema for the module (e.g. "customers"). Used to isolate tables.</summary>
    string DatabaseSchema { get; }

    void AddServices(IServiceCollection services, IConfiguration configuration);
}

public interface IEndpointModule : IModule
{
    void MapEndpoints(IEndpointRouteBuilderAdapter endpoints);
}

public interface IEndpointRouteBuilderAdapter
{
    object UnderlyingBuilder { get; }
}
