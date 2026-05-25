namespace BuildingBlocks.Auditing.Entities;

/// <summary>
/// One row per property modified on an entity within an operation.
/// Values are clipped to the configured maximum length.
/// </summary>
public sealed class EntityPropertyChange
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid EntityChangeId { get; set; }

    public string PropertyName { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;

    public string? OriginalValue { get; set; }
    public string? NewValue { get; set; }
}
