using BuildingBlocks.SharedKernel;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Presentation.Endpoints;

/// <summary>
/// Translates a domain/application <see cref="Result"/> into an appropriate HTTP response,
/// using <see cref="ProblemDetails"/> for failures (RFC 7807).
/// </summary>
public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result, Func<IResult>? onSuccess = null) =>
        result.IsSuccess
            ? (onSuccess?.Invoke() ?? Results.NoContent())
            : ToProblem(result.Error);

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null) =>
        result.IsSuccess
            ? (onSuccess?.Invoke(result.Value) ?? Results.Ok(result.Value))
            : ToProblem(result.Error);

    private static IResult ToProblem(Error error)
    {
        var (status, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Validation failed"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
            _ => (StatusCodes.Status500InternalServerError, "An error occurred")
        };

        return Results.Problem(
            title: title,
            detail: error.Message,
            statusCode: status,
            type: $"https://errors.emm/{error.Code}",
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
