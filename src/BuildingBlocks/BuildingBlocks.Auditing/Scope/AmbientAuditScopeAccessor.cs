using BuildingBlocks.Auditing.Abstractions;

namespace BuildingBlocks.Auditing.Scope;

/// <summary>
/// AsyncLocal-backed scope stack. Mirrors the UoW accessor pattern — Push when
/// starting a request/command, Pop when finishing. The interceptor reads
/// <see cref="Current"/> at SaveChanges time to attach captured changes.
/// </summary>
public sealed class AmbientAuditScopeAccessor : IAuditScopeAccessor
{
    private static readonly AsyncLocal<ScopeHolder> Holder = new();

    public AuditScope? Current => Holder.Value?.Current;

    public AuditScope Push(string action)
    {
        Holder.Value ??= new ScopeHolder();
        var scope = new AuditScope { Action = action };
        Holder.Value.Push(scope);
        return scope;
    }

    public void Pop(AuditScope scope) => Holder.Value?.Pop(scope);

    private sealed class ScopeHolder
    {
        private readonly Stack<AuditScope> _stack = new();
        public AuditScope? Current => _stack.Count == 0 ? null : _stack.Peek();
        public void Push(AuditScope s) => _stack.Push(s);
        public void Pop(AuditScope s)
        {
            if (_stack.Count > 0 && ReferenceEquals(_stack.Peek(), s))
                _stack.Pop();
        }
    }
}
