using BuildingBlocks.Application.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Presentation.Middleware;

/// <summary>
/// Opens one ambient unit of work for each mutating HTTP request and completes it
/// before the response starts, so all module DbContexts commit or roll back together.
/// </summary>
public sealed class UnitOfWorkMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UnitOfWorkMiddleware> _logger;

    public UnitOfWorkMiddleware(RequestDelegate next, ILogger<UnitOfWorkMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context, IUnitOfWorkManager unitOfWorkManager)
    {
        if (!ShouldCreateTransactionalUnitOfWork(context.Request.Method))
        {
            await _next(context);
            return;
        }

        await using var unitOfWork = await unitOfWorkManager.BeginAsync(cancellationToken: context.RequestAborted);
        var completion = new UnitOfWorkCompletion(context, unitOfWork, _logger);

        context.Response.OnStarting(static state =>
        {
            var completionState = (UnitOfWorkCompletion)state;
            return completionState.FinishAsync();
        }, completion);

        try
        {
            await _next(context);

            if (!completion.IsFinished)
            {
                await completion.FinishAsync();
            }
        }
        catch
        {
            if (!completion.IsFinished)
            {
                await unitOfWork.RollbackAsync(CancellationToken.None);
                completion.MarkFinished();
            }

            throw;
        }
    }

    private static bool ShouldCreateTransactionalUnitOfWork(string method) =>
        HttpMethods.IsPost(method) ||
        HttpMethods.IsPut(method) ||
        HttpMethods.IsPatch(method) ||
        HttpMethods.IsDelete(method);

    private sealed class UnitOfWorkCompletion
    {
        private readonly HttpContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;

        public UnitOfWorkCompletion(HttpContext context, IUnitOfWork unitOfWork, ILogger logger)
        {
            _context = context;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public bool IsFinished { get; private set; }

        public async Task FinishAsync()
        {
            if (IsFinished) return;

            if (_context.Response.StatusCode < StatusCodes.Status400BadRequest)
            {
                await _unitOfWork.CompleteAsync(_context.RequestAborted);
            }
            else
            {
                _logger.LogDebug(
                    "Rolling back UoW {UoWId} because the response status is {StatusCode}",
                    _unitOfWork.Id,
                    _context.Response.StatusCode);
                await _unitOfWork.RollbackAsync(CancellationToken.None);
            }

            IsFinished = true;
        }

        public void MarkFinished() => IsFinished = true;
    }
}

