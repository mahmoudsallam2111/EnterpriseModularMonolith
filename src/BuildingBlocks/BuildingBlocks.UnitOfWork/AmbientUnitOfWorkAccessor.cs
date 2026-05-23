using BuildingBlocks.Application.UnitOfWork;

namespace BuildingBlocks.UnitOfWork;

/// <summary>
/// AsyncLocal-backed ambient context. Holds a stack of nested units of work so the
/// current one is always reachable from any layer (handlers, repositories, interceptors)
/// without passing it as a parameter.
/// </summary>
public sealed class AmbientUnitOfWorkAccessor : IUnitOfWorkAccessor
{
    private static readonly AsyncLocal<UnitOfWorkHolder> Holder = new();

    public IUnitOfWork? Current => Holder.Value?.Current;

    public void Push(IUnitOfWork uow)
    {
        Holder.Value ??= new UnitOfWorkHolder();
        Holder.Value.Push(uow);
    }

    public void Pop(IUnitOfWork uow) => Holder.Value?.Pop(uow);

    private sealed class UnitOfWorkHolder
    {
        private readonly Stack<IUnitOfWork> _stack = new();
        public IUnitOfWork? Current => _stack.Count == 0 ? null : _stack.Peek();
        public void Push(IUnitOfWork uow) => _stack.Push(uow);
        public void Pop(IUnitOfWork uow)
        {
            if (_stack.Count > 0 && ReferenceEquals(_stack.Peek(), uow))
                _stack.Pop();
        }
    }
}
