using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.Application.Security;
using BuildingBlocks.SharedKernel;
using Users.Contracts;

namespace Users.Application.Users.Queries.GetMe;

public sealed record GetMeQuery : IQuery<UserSummaryDto>;

internal sealed class GetMeQueryHandler : IQueryHandler<GetMeQuery, UserSummaryDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserReadModel _readModel;

    public GetMeQueryHandler(ICurrentUser currentUser, IUserReadModel readModel)
    {
        _currentUser = currentUser;
        _readModel = readModel;
    }

    public async Task<Result<UserSummaryDto>> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Error.Unauthorized("Auth.NotAuthenticated", "Authentication required.");

        var dto = await _readModel.GetSummaryAsync(_currentUser.UserId.Value, cancellationToken);
        return dto is null
            ? Error.NotFound("Users.NotFound", $"User {_currentUser.UserId} not found.")
            : dto;
    }
}

public interface IUserReadModel
{
    Task<UserSummaryDto?> GetSummaryAsync(Guid userId, CancellationToken cancellationToken);
}
