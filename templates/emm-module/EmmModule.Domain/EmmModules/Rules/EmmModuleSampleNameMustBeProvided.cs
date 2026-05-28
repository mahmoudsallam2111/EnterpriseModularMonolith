using BuildingBlocks.Domain;

namespace EmmModule.Domain.EmmModules.Rules;

public sealed class EmmModuleSampleNameMustBeProvided : IBusinessRule
{
    private readonly string? _name;
    public EmmModuleSampleNameMustBeProvided(string? name) => _name = name;
    public string Code => "EmmModule.NameRequired";
    public string Message => "Name must be provided.";
    public bool IsBroken() => string.IsNullOrWhiteSpace(_name);
}
