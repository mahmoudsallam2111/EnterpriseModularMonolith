using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.EventBus;
using BuildingBlocks.SharedKernel;
using EmmModule.Domain.EmmModules;
using EmmModule.IntegrationEvents;
using FluentValidation;

namespace EmmModule.Application.EmmModules.Commands.CreateSample;

[RequiresPermission(EmmModulePermissions.Manage)]
public sealed record CreateEmmModuleSampleCommand(string Name) : ICommand<Guid>;

public sealed class CreateEmmModuleSampleCommandValidator : AbstractValidator<CreateEmmModuleSampleCommand>
{
    public CreateEmmModuleSampleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

internal sealed class CreateEmmModuleSampleCommandHandler
    : ICommandHandler<CreateEmmModuleSampleCommand, Guid>
{
    private readonly IEmmModuleSampleRepository _repository;
    private readonly IIntegrationEventQueue _integrationEventQueue;
    private readonly IClock _clock;

    public CreateEmmModuleSampleCommandHandler(
        IEmmModuleSampleRepository repository,
        IIntegrationEventQueue integrationEventQueue,
        IClock clock)
    {
        _repository = repository;
        _integrationEventQueue = integrationEventQueue;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(CreateEmmModuleSampleCommand request, CancellationToken cancellationToken)
    {
        var sample = EmmModuleSample.Create(request.Name);
        await _repository.AddAsync(sample, cancellationToken);

        _integrationEventQueue.Enqueue(new EmmModuleSampleCreatedIntegrationEvent(
            sample.Id.Value, sample.Name, _clock.UtcNow));

        return sample.Id.Value;
    }
}
