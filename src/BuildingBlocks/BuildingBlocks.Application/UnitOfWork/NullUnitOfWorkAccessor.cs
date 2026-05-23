namespace BuildingBlocks.Application.UnitOfWork;

internal sealed class NullUnitOfWorkAccessor : IUnitOfWorkAccessor
{
    public IUnitOfWork? Current => null;
}

