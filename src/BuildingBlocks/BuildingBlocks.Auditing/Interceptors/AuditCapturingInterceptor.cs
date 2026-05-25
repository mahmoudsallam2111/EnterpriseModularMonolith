using BuildingBlocks.Auditing.Abstractions;
using BuildingBlocks.Auditing.Sanitisation;
using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Auditing.Interceptors;

/// <summary>
/// Attached to every business module DbContext. Runs in <see cref="SavingChangesAsync"/>:
/// walks the change tracker, finds entries for <see cref="IAuditedEntity"/> entities,
/// snapshots original vs. current values, and attaches the diffs to the current
/// <see cref="AuditScope"/>. Knows nothing about WHEN the audit row is written —
/// that's the writer's job.
/// </summary>
public sealed class AuditCapturingInterceptor : SaveChangesInterceptor
{
    private readonly IAuditScopeAccessor _scopeAccessor;
    private readonly AuditingOptions _options;

    public AuditCapturingInterceptor(IAuditScopeAccessor scopeAccessor, IOptions<AuditingOptions> options)
    {
        _scopeAccessor = scopeAccessor;
        _options = options.Value;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (_options.Enabled && eventData.Context is not null)
            Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (_options.Enabled && eventData.Context is not null)
            Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void Capture(DbContext context)
    {
        var scope = _scopeAccessor.Current;
        if (scope is null) return;

        var module = DeriveModuleName(context);

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IAuditedEntity) continue;

            var clrType = entry.Entity.GetType();
            var typeName = clrType.FullName ?? clrType.Name;

            // Class-level opt out
            if (Attribute.IsDefined(clrType, typeof(DisableAuditingAttribute))) continue;
            if (_options.ExcludedEntityTypes.Contains(typeName, StringComparer.Ordinal)) continue;

            var changeType = entry.State switch
            {
                EntityState.Added => (int?)0,
                EntityState.Modified => 1,
                EntityState.Deleted => 2,
                _ => null
            };
            if (changeType is null) continue;

            var pending = new PendingEntityChange
            {
                EntityType = typeName,
                EntityId = BuildEntityId(entry),
                Module = module,
                ChangeType = changeType.Value,
                ChangeTime = DateTimeOffset.UtcNow,
            };

            foreach (var prop in entry.Properties)
            {
                if (prop.Metadata.IsShadowProperty()) continue;
                if (prop.Metadata.IsPrimaryKey() && entry.State != EntityState.Modified) { /* keep key on created/deleted */ }

                var name = prop.Metadata.Name;
                var clrPropInfo = clrType.GetProperty(name);
                var isClassDisabled = clrPropInfo?.GetCustomAttributes(typeof(DisableAuditingAttribute), true).Length > 0;
                var masked = isClassDisabled || ParameterSanitiser.IsSensitiveName(name, _options.SensitivePropertyNames);

                string? original = null, current = null;
                switch (entry.State)
                {
                    case EntityState.Added:
                        current = Stringify(prop.CurrentValue);
                        break;
                    case EntityState.Deleted:
                        original = Stringify(prop.OriginalValue);
                        break;
                    case EntityState.Modified:
                        if (!prop.IsModified) continue;
                        original = Stringify(prop.OriginalValue);
                        current = Stringify(prop.CurrentValue);
                        if (string.Equals(original, current, StringComparison.Ordinal)) continue;
                        break;
                }

                pending.Properties.Add(new PendingPropertyChange
                {
                    PropertyName = name,
                    PropertyType = prop.Metadata.ClrType.FullName ?? prop.Metadata.ClrType.Name,
                    OriginalValue = ParameterSanitiser.Clip(masked ? "***" : original, _options.MaxPropertyValueLength),
                    NewValue = ParameterSanitiser.Clip(masked ? "***" : current, _options.MaxPropertyValueLength)
                });
            }

            // For Modified rows with no actual property diffs (e.g. owned collection rewrite),
            // skip — nothing meaningful to record.
            if (changeType == 1 && pending.Properties.Count == 0) continue;

            scope.EntityChanges.Add(pending);
        }
    }

    private static string BuildEntityId(EntityEntry entry)
    {
        var pkProperties = entry.Metadata.FindPrimaryKey()?.Properties;
        if (pkProperties is null || pkProperties.Count == 0) return string.Empty;
        if (pkProperties.Count == 1)
            return Stringify(entry.Property(pkProperties[0].Name).CurrentValue) ?? string.Empty;
        return string.Join('|', pkProperties.Select(p => Stringify(entry.Property(p.Name).CurrentValue) ?? "?"));
    }

    private static string? Stringify(object? value) => value switch
    {
        null => null,
        string s => s,
        DateTime dt => dt.ToString("O"),
        DateTimeOffset dto => dto.ToString("O"),
        Guid g => g.ToString(),
        Enum e => e.ToString(),
        IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString()
    };

    private static string? DeriveModuleName(DbContext context)
    {
        var name = context.GetType().Name;
        if (name.EndsWith("DbContext", StringComparison.Ordinal))
            return name[..^"DbContext".Length];
        return name;
    }
}
