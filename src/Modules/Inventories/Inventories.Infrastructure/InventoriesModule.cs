using BuildingBlocks.Application.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventories.Infrastructure;

public sealed class InventoriesModule : IModule
{
    public string Name => "Inventories";
    public string DatabaseSchema => "inventories";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        // Services will be added here
    }
}
