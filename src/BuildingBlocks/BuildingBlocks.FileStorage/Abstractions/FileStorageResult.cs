namespace BuildingBlocks.FileStorage.Abstractions;

/// <summary>Returned from <see cref="IFileStore.UploadAsync"/> after a successful write.</summary>
public sealed record FileStorageResult(
    string Container,
    string ObjectKey,
    long SizeBytes,
    string ContentType,
    string? ETag);
