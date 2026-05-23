using BuildingBlocks.SharedKernel;
using MediatR;

namespace BuildingBlocks.Application.Cqrs;

/// <summary>
/// A command that mutates state. Returns a <see cref="Result"/> indicating success or a typed error.
/// </summary>
public interface ICommand : IRequest<Result>;

/// <summary>
/// A command that mutates state and returns a value (e.g. the id of the created resource).
/// </summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;
