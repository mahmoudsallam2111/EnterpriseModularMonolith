namespace BuildingBlocks.FileStorage.Abstractions;

/// <summary>
/// Snapshot returned from <see cref="IFileStore.GetInfoAsync"/> / <see cref="IFileStore.ListAsync"/>.
/// Does not include the file body — call <see cref="IFileStore.DownloadAsync"/> for that.
/// </summary>
public sealed record FileStorageInfo(
    string Container,
    string ObjectKey,
    long SizeBytes,
    string ContentType,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastModifiedUtc,
    string? ETag,
    IReadOnlyDictionary<string, string>? CustomMetadata);
