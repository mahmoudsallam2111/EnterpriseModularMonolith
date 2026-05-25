namespace BuildingBlocks.Domain;

/// <summary>
/// Marker interface for entities that should be tracked by the audit subsystem.
/// Implementations have their Created / Updated / Deleted state captured into the
/// audit database along with per-property original/new value diffs.
///
/// Lives in BuildingBlocks.Domain (not Auditing) so domain entities can opt in
/// without taking a dependency on the audit infrastructure project.
/// </summary>
public interface IAuditedEntity;

/// <summary>
/// Applied to an entity class to opt it out of audit capture entirely, or to a
/// property to mask its value (passwords, tokens, signing keys, sensitive PII).
/// When applied to a property the property name is still recorded but the
/// original / new values are replaced with <c>"***"</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class DisableAuditingAttribute : Attribute;
