using BuildingBlocks.FileStorage.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.FileStorage;

/// <summary>
/// Decorator over the chosen <see cref="IFileStore"/>. On every UploadAsync:
///   1. runs each registered <see cref="IFileValidator"/> (size, content-type, ...) — first failure rejects,
///   2. when <c>VirusScanEnabled</c>, buffers the upload, scans, and only then forwards to the provider.
/// All other operations pass straight through.
/// </summary>
public sealed class ValidatedFileStore : IFileStore
{
    private readonly IFileStore _inner;
    private readonly IEnumerable<IFileValidator> _validators;
    private readonly IFileScanner _scanner;
    private readonly FileStorageOptions _options;
    private readonly ILogger<ValidatedFileStore> _logger;

    public ValidatedFileStore(
        IFileStore inner,
        IEnumerable<IFileValidator> validators,
        IFileScanner scanner,
        IOptions<FileStorageOptions> options,
        ILogger<ValidatedFileStore> logger)
    {
        _inner = inner;
        _validators = validators;
        _scanner = scanner;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FileStorageResult> UploadAsync(string container, string objectKey, Stream content, FileMetadata metadata, CancellationToken cancellationToken = default)
    {
        // We may need to read the stream twice (scan + store). Buffer if not seekable.
        Stream working = content;
        long size = content.CanSeek ? content.Length : -1;

        if (_options.VirusScanEnabled || size < 0)
        {
            var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            working = buffer;
            size = buffer.Length;
        }

        // Validate.
        var candidate = new FileUploadCandidate(container, objectKey, metadata, size);
        foreach (var v in _validators)
        {
            var r = await v.ValidateAsync(candidate, cancellationToken);
            if (!r.IsValid)
            {
                _logger.LogWarning("File upload rejected by {Validator}: {Code} — {Reason}", v.GetType().Name, r.Code, r.Reason);
                throw r.Code switch
                {
                    "FileStorage.TooLarge" => new FileTooLargeException(size, _options.MaxFileSizeBytes),
                    "FileStorage.DisallowedContentType" => new DisallowedContentTypeException(metadata.ContentType),
                    _ => new FileStorageException(r.Code ?? "FileStorage.Invalid", r.Reason ?? "Invalid upload.")
                };
            }
        }

        // Scan.
        if (_options.VirusScanEnabled)
        {
            var scan = await _scanner.ScanAsync(working, objectKey, cancellationToken);
            if (!scan.IsClean)
            {
                _logger.LogWarning("File upload rejected by scanner {Scanner}: {Threat}", scan.Scanner, scan.ThreatName);
                throw new FileScanRejectedException(scan.ThreatName);
            }
            if (working.CanSeek) working.Position = 0;
        }

        return await _inner.UploadAsync(container, objectKey, working, metadata, cancellationToken);
    }

    public Task<Stream?> DownloadAsync(string container, string objectKey, CancellationToken cancellationToken = default)
        => _inner.DownloadAsync(container, objectKey, cancellationToken);
    public Task<FileStorageInfo?> GetInfoAsync(string container, string objectKey, CancellationToken cancellationToken = default)
        => _inner.GetInfoAsync(container, objectKey, cancellationToken);
    public Task<bool> ExistsAsync(string container, string objectKey, CancellationToken cancellationToken = default)
        => _inner.ExistsAsync(container, objectKey, cancellationToken);
    public Task<bool> DeleteAsync(string container, string objectKey, CancellationToken cancellationToken = default)
        => _inner.DeleteAsync(container, objectKey, cancellationToken);
    public Task<IReadOnlyList<FileStorageInfo>> ListAsync(string container, string? prefix = null, int maxResults = 200, CancellationToken cancellationToken = default)
        => _inner.ListAsync(container, prefix, maxResults, cancellationToken);
    public Task<Uri> GetPresignedUrlAsync(string container, string objectKey, TimeSpan expiry, PresignedUrlPurpose purpose, CancellationToken cancellationToken = default)
        => _inner.GetPresignedUrlAsync(container, objectKey, expiry, purpose, cancellationToken);
    public Task<PresignedUploadTicket> CreateUploadTicketAsync(string container, string objectKey, TimeSpan expiry, FileMetadata metadata, CancellationToken cancellationToken = default)
        => _inner.CreateUploadTicketAsync(container, objectKey, expiry, metadata, cancellationToken);
    public Uri? GetPublicUrl(string container, string objectKey) => _inner.GetPublicUrl(container, objectKey);
}
