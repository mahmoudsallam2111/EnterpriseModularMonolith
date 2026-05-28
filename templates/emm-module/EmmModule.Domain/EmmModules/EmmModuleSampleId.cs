using BuildingBlocks.Domain;

namespace EmmModule.Domain.EmmModules;

public sealed record EmmModuleSampleId(Guid Value) : StronglyTypedId<EmmModuleSampleId>(Value)
{
    public static EmmModuleSampleId New() => new(Guid.NewGuid());
    public static EmmModuleSampleId From(Guid value) => new(value);
}
