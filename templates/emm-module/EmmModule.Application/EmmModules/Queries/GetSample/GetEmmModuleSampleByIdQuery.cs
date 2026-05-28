using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.SharedKernel;
using EmmModule.Contracts;

namespace EmmModule.Application.EmmModules.Queries.GetSample;

public sealed record GetEmmModuleSampleByIdQuery(Guid Id) : IQuery<EmmModuleSummaryDto>;

internal sealed class GetEmmModuleSampleByIdQueryHandler
    : IQueryHandler<GetEmmModuleSampleByIdQuery, EmmModuleSummaryDto>
{
    private readonly IEmmModuleReadModel _readModel;
    public GetEmmModuleSampleByIdQueryHandler(IEmmModuleReadModel readModel) => _readModel = readModel;

    public async Task<Result<EmmModuleSummaryDto>> Handle(GetEmmModuleSampleByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _readModel.GetByIdAsync(request.Id, cancellationToken);
        return dto is null
            ? Error.NotFound("EmmModule.NotFound", $"EmmModuleSample {request.Id} not found.")
            : dto;
    }
}

public interface IEmmModuleReadModel
{
    Task<EmmModuleSummaryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
