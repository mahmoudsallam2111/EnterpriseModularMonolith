namespace BuildingBlocks.FileStorage.Abstractions;

/// <summary>
/// Provider-agnostic file storage. The default <c>LocalFileStore</c> writes to disk;
/// <c>S3FileStore</c> / <c>AzureBlobFileStore</c> are drop-in alternatives that emit
/// the same shapes. Modules consume this interface and never see the concrete provider.
/// </summary>
public interface IFileStore
{
    Task<FileStorageResult> UploadAsync(string container, string objectKey, Stream content, FileMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>Returns null when the object is missing. Caller MUST dispose the stream.</summary>
    Task<Stream?> DownloadAsync(string container, string objectKey, CancellationToken cancellationToken = default);

    Task<FileStorageInfo?> GetInfoAsync(string container, string objectKey, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string container, string objectKey, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string container, string objectKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FileStorageInfo>> ListAsync(string container, string? prefix = null, int maxResults = 200, CancellationToken cancellationToken = default);

    /// <summary>Time-limited URL for direct read or write. Implementations decide signing scheme.</summary>
    Task<Uri> GetPresignedUrlAsync(string container, string objectKey, TimeSpan expiry, PresignedUrlPurpose purpose, CancellationToken cancellationToken = default);

    /// <summary>Presigned ticket for direct browser-to-store uploads.</summary>
    Task<PresignedUploadTicket> CreateUploadTicketAsync(string container, string objectKey, TimeSpan expiry, FileMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>Returns a public URL if the object is publicly accessible, otherwise null.</summary>
    Uri? GetPublicUrl(string container, string objectKey);
}
