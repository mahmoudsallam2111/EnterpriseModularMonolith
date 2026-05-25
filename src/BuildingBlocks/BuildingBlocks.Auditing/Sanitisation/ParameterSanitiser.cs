using System.Reflection;
using System.Text.Json;
using BuildingBlocks.Auditing.Abstractions;
using BuildingBlocks.Domain;

namespace BuildingBlocks.Auditing.Sanitisation;

/// <summary>
/// Sanitises an object graph for safe audit capture:
///  - masks properties whose name matches any configured sensitive token,
///  - masks properties marked with <see cref="DisableAuditingAttribute"/>,
///  - returns a JSON string ready to write into the <c>parameters</c> column.
///
/// Never throws; on any failure returns <c>"{}"</c> so audit can't break the request.
/// </summary>
public static class ParameterSanitiser
{
    private const string Masked = "***";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static string ToJson(object? value, AuditingOptions options)
    {
        if (value is null) return "null";
        try
        {
            var sanitised = Sanitise(value, options.SensitivePropertyNames);
            return JsonSerializer.Serialize(sanitised, JsonOptions);
        }
        catch
        {
            return "{}";
        }
    }

    public static string? MaskPropertyValue(string? value, string propertyName, AuditingOptions options) =>
        IsSensitiveName(propertyName, options.SensitivePropertyNames) ? Masked : value;

    public static bool IsSensitiveName(string propertyName, IReadOnlyCollection<string> tokens)
    {
        foreach (var t in tokens)
            if (propertyName.Contains(t, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static object? Sanitise(object value, IReadOnlyCollection<string> tokens)
    {
        var type = value.GetType();
        if (type.IsPrimitive || value is string || value is decimal || value is DateTime || value is DateTimeOffset || value is Guid || value is Enum)
            return value;

        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0) continue;
            if (prop.GetCustomAttribute<DisableAuditingAttribute>() is not null)
            {
                result[prop.Name] = Masked;
                continue;
            }
            if (IsSensitiveName(prop.Name, tokens))
            {
                result[prop.Name] = Masked;
                continue;
            }

            object? v;
            try { v = prop.GetValue(value); }
            catch { continue; }

            result[prop.Name] = v;
        }
        return result;
    }

    public static string? Clip(string? value, int max)
    {
        if (value is null) return null;
        return value.Length <= max ? value : value[..max];
    }
}
