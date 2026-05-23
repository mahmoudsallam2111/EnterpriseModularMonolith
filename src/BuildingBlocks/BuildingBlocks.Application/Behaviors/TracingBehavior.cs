using System.Diagnostics;
using MediatR;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// Creates an OpenTelemetry activity for every request so the handler shows up
/// as a span in distributed traces alongside the EF Core and HTTP spans.
/// </summary>
public sealed class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ActivitySource ActivitySource = new("EMM.Application");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity(
            $"Mediator {typeof(TRequest).Name}",
            ActivityKind.Internal);
        activity?.SetTag("mediator.request", typeof(TRequest).FullName);
        try
        {
            var response = await next();
            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }
}
