using BuildingBlocks.FileStorage.Abstractions;

namespace BuildingBlocks.FileStorage.S3;

/// <summary>
/// Placeholder. Real implementation should reference AWSSDK.S3 (or MinIO client) and
/// translate <see cref="IFileStore"/> calls to PutObject / GetObject / etc.
/// Kept as a stub so the abstraction surface is complete and consumers can swap providers later.
/// </summary>
public sealed class S3FileStore : IFileStore
{
    private const string NotImplementedMessage =
        "S3FileStore is a placeholder. Add AWSSDK.S3 / Minio.AspNetCore and implement these calls.";

    public Task<FileStorageResult> UploadAsync(string container, string objectKey, Stream content, FileMetadata metadata, CancellationToken cancellationToken = default)
        => throw new NotImplementedException(NotImplementedMessage);

    public Task<Stream?> DownloadAsync(string container, string objectKey, CancellationToken cancellationToken = default)
        => throw new NotImplementedException(NotImplementedMessage);

    public Task<FileStorageInfo?> GetInfoAsync(string container, string objectKey, CancellationToken cancellationToken = default)
        => throw new NotImplementedException(NotImplementedMessage);

    public Task<bool> ExistsAsync(string container, string objectKey, CancellationToken cancellationToken = default)
        => throw new NotImplementedException(NotImplementedMessage);

    public Task<bool> DeleteAsync(string container, string objectKey, CancellationToken cancellationToken = default)
        => throw new NotImplementedException(NotImplementedMessage);

    public Task<IReadOnlyList<FileStorageInfo>> ListAsync(string container, string? prefix = null, int maxResults = 200, CancellationToken cancellationToken = default)
        => throw new NotImplementedException(NotImplementedMessage);

    public Task<Uri> GetPresignedUrlAsync(string container, string objectKey, TimeSpan expiry, PresignedUrlPurpose purpose, CancellationToken cancellationToken = default)
        => throw new NotImplementedException(NotImplementedMessage);

    public Task<PresignedUploadTicket> CreateUploadTicketAsync(string container, string objectKey, TimeSpan expiry, FileMetadata metadata, CancellationToken cancellationToken = default)
        => throw new NotImplementedException(NotImplementedMessage);

    public Uri? GetPublicUrl(string container, string objectKey) => null;
}
