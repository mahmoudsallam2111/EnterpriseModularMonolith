namespace BuildingBlocks.Domain;

/// <summary>
/// Base class for entities that have an identity. Equality is identity-based.
/// </summary>
public abstract class Entity<TId>
    where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    protected Entity(TId id) => Id = id;

    // EF Core
#pragma warning disable CS8618
    protected Entity() { }
#pragma warning restore CS8618

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        if (EqualityComparer<TId>.Default.Equals(Id, default!) ||
            EqualityComparer<TId>.Default.Equals(other.Id, default!)) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? a, Entity<TId>? b) => Equals(a, b);
    public static bool operator !=(Entity<TId>? a, Entity<TId>? b) => !Equals(a, b);
}
