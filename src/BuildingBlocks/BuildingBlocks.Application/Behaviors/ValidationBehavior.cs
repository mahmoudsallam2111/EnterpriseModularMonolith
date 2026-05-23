using System.Reflection;
using BuildingBlocks.SharedKernel;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// Runs all registered FluentValidation validators for the request. On failure, returns
/// a <see cref="Result"/> populated with a typed validation error rather than throwing.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToArray();

        if (failures.Length == 0)
            return await next();

        return BuildFailureResponse(failures);
    }

    private static TResponse BuildFailureResponse(ValidationFailure[] failures)
    {
        var firstFailure = failures[0];
        var error = Error.Validation(
            firstFailure.ErrorCode ?? "Validation.Failed",
            string.Join("; ", failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}")));

        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
            return (TResponse)(object)Result.Failure(error);

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = typeof(Result)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(m => m.Name == nameof(Result.Failure) && m.IsGenericMethod);
            var generic = failureMethod.MakeGenericMethod(responseType.GetGenericArguments()[0]);
            return (TResponse)generic.Invoke(null, [error])!;
        }

        throw new InvalidOperationException(
            $"ValidationBehavior only supports Result/Result<T> handler return types. Got {responseType.FullName}.");
    }
}
