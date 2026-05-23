using System.Reflection;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.UnitOfWork;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationPipeline(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assemblies);
        });

        services.TryAddSingleton<IUnitOfWorkAccessor, NullUnitOfWorkAccessor>();

        // Order matters: first registered is outermost.
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>)));
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(TracingBehavior<,>)));
       // services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>)));
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)));

        // Unit of work is now handled by request-scoped middleware.
        //services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>)));

        services.AddValidatorsFromAssemblies(assemblies, includeInternalTypes: true);

        return services;
    }
}
