using BuildingBlocks.Application.Modules;
using Customers.Infrastructure;
using Orders.Infrastructure;
using Inventories.Infrastructure;

namespace EnterpriseModularMonolith.Api.Composition;

/// <summary>
/// The set of modules composed by the host. Adding a new module = adding one line here.
/// The rest of the boot pipeline iterates the list and asks each module to register itself.
/// </summary>
internal static class ModuleRegistry
{
    public static IReadOnlyList<IModule> All { get; } = new IModule[]
    {
        new CustomersModule(),
        new OrdersModule(),
        new InventoriesModule(),
    };
}
