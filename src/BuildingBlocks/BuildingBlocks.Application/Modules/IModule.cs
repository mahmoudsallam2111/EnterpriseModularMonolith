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

/// <summary>
/// Modules that expose HTTP endpoints implement this interface so the host can
/// invoke their endpoint mapping without taking a reference to the module assembly.
/// </summary>
public interface IEndpointModule : IModule
{
    void MapEndpoints(IEndpointRouteBuilderAdapter endpoints);
}

/// <summary>
/// Tiny adapter so module assemblies don't need a direct reference to
/// Microsoft.AspNetCore.Http and can stay framework-light.
/// </summary>
public interface IEndpointRouteBuilderAdapter
{
    object UnderlyingBuilder { get; }
}
