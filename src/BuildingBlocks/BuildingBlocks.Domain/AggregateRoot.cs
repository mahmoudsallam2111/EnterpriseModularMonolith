namespace BuildingBlocks.Domain;

/// <summary>
/// Base class for aggregate roots. Aggregates are the consistency boundary in DDD —
/// they own a set of entities, enforce invariants, and emit domain events.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Concurrency token (PostgreSQL xmin column). Modified by the infrastructure on save.
    /// </summary>
    public uint Version { get; protected set; }

    protected AggregateRoot(TId id) : base(id) { }
    protected AggregateRoot() { }

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected static void CheckRule(IBusinessRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (rule.IsBroken())
            throw new BusinessRuleValidationException(rule);
    }
}

/// <summary>
/// Marker for aggregate roots — used by the infrastructure (interceptors, repositories,
/// event dispatcher) without exposing the generic type parameter.
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
