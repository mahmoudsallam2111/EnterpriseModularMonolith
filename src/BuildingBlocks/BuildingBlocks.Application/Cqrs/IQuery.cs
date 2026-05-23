using BuildingBlocks.SharedKernel;
using MediatR;

namespace BuildingBlocks.Application.Cqrs;

/// <summary>
/// A read-only query. Always returns a result wrapping the response type.
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
