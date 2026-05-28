using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Application.DataFiltering;

public static class DataFilterRegistration
{
    /// <summary>
    /// Registers <see cref="IDataFilter"/> (singleton, AsyncLocal-scoped state) and the
    /// typed <see cref="IDataFilter{TFilter}"/> wrapper for arbitrary T. Safe to call
    /// multiple times.
    /// </summary>
    public static IServiceCollection AddDataFiltering(this IServiceCollection services)
    {
        services.TryAddSingleton<IDataFilter, AmbientDataFilter>();
        services.TryAdd(ServiceDescriptor.Singleton(typeof(IDataFilter<>), typeof(DataFilter<>)));
        return services;
    }
}
