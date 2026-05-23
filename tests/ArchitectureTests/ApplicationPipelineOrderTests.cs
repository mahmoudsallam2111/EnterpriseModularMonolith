using BuildingBlocks.Application;
using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.Application.Security;
using BuildingBlocks.SharedKernel;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArchitectureTests;

public sealed class ApplicationPipelineOrderTests
{
    [Fact]
    public async Task Authorization_should_short_circuit_before_validation()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ICurrentUser, AnonymousCurrentUser>();
        services.AddSingleton<IPermissionService, AllowAllPermissionService>();
        services.AddSingleton<PipelineProbe>();
        services.AddApplicationPipeline(typeof(ApplicationPipelineOrderTests).Assembly);

        using var provider = services.BuildServiceProvider();

        var result = await provider
            .GetRequiredService<ISender>()
            .Send(new ProtectedCommand(""));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.NotAuthenticated");
        provider.GetRequiredService<PipelineProbe>().ValidationRan.Should().BeFalse();
    }
}

internal sealed class PipelineProbe
{
    public bool ValidationRan { get; set; }
}

[RequiresPermission("tests.protected")]
internal sealed record ProtectedCommand(string Name) : ICommand;

internal sealed class ProtectedCommandHandler : ICommandHandler<ProtectedCommand>
{
    public Task<Result> Handle(ProtectedCommand request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success());
}

internal sealed class ProtectedCommandValidator : AbstractValidator<ProtectedCommand>
{
    public ProtectedCommandValidator(PipelineProbe probe)
    {
        RuleFor(x => x.Name).Custom((_, context) =>
        {
            probe.ValidationRan = true;
            context.AddFailure("Name", "Name is required.");
        });
    }
}

internal sealed class AnonymousCurrentUser : ICurrentUser
{
    public bool IsAuthenticated => false;
    public Guid? UserId => null;
    public string? UserName => null;
    public string? Email => null;
    public IReadOnlyCollection<string> Roles => Array.Empty<string>();
    public IReadOnlyCollection<string> Permissions => Array.Empty<string>();
    public Guid? TenantId => null;
    public string? GetClaim(string type) => null;
}

internal sealed class AllowAllPermissionService : IPermissionService
{
    public Task<bool> HasPermissionAsync(
        Guid userId,
        string permission,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
}
