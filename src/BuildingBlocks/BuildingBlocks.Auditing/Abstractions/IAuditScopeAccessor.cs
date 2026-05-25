namespace BuildingBlocks.Auditing.Abstractions;

/// <summary>
/// Ambient access to the audit scope currently open on this logical async flow.
/// Used by <see cref="Interceptors.AuditCapturingInterceptor"/> to attach captured
/// entity changes without having to receive a parameter on every SaveChanges call.
/// </summary>
public interface IAuditScopeAccessor
{
    AuditScope? Current { get; }
    AuditScope Push(string action);
    void Pop(AuditScope scope);
}
