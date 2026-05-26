using System.Collections.ObjectModel;

namespace BuildingBlocks.FileStorage.Abstractions;

/// <summary>
/// Caller-supplied metadata describing the file being stored. <c>ContentType</c> is
/// authoritative; custom tags are surfaced back through <see cref="FileStorageInfo"/>.
/// </summary>
public sealed record FileMetadata(
    string ContentType,
    string? OriginalFileName = null,
    IReadOnlyDictionary<string, string>? CustomMetadata = null)
{
    public static FileMetadata Of(string contentType) => new(contentType);
}
