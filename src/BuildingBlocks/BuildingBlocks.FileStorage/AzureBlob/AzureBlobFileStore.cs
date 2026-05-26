using BuildingBlocks.FileStorage.Abstractions;

namespace BuildingBlocks.FileStorage.AzureBlob;

/// <summary>
/// Placeholder. Real implementation should reference Azure.Storage.Blobs and
/// translate <see cref="IFileStore"/> calls to BlobClient.UploadAsync / GenerateSasUri / etc.
/// </summary>
public sealed class AzureBlobFileStore : IFileStore
{
    private const string NotImplementedMessage =
        "AzureBlobFileStore is a placeholder. Add Azure.Storage.Blobs and implement these calls.";

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
