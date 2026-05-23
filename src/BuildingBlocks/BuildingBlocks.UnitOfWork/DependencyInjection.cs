using BuildingBlocks.Application.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.UnitOfWork;

public static class DependencyInjection
{
    public static IServiceCollection AddAmbientUnitOfWork(this IServiceCollection services)
    {
        services.AddSingleton<AmbientUnitOfWorkAccessor>();
        services.AddSingleton<IUnitOfWorkAccessor>(sp => sp.GetRequiredService<AmbientUnitOfWorkAccessor>());
        services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
        return services;
    }
}
