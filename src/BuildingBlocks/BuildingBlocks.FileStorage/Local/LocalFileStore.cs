using System.Collections.ObjectModel;
using System.Text.Json;
using BuildingBlocks.FileStorage.Abstractions;
using BuildingBlocks.FileStorage.Hashing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.FileStorage.Local;

/// <summary>
/// Disk-backed file store. Each container becomes a subdirectory of
/// <see cref="LocalFileStorageOptions.RootPath"/>; each object becomes a file
/// plus a sidecar ".meta.json" carrying content-type and custom metadata.
/// Presigned URLs are HMAC-signed query strings that the FileEndpoints honour.
/// </summary>
public sealed class LocalFileStore : IFileStore
{
    private readonly FileStorageOptions _options;
    private readonly PresignedUrlSigner _signer;
    private readonly ILogger<LocalFileStore> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public LocalFileStore(IOptions<FileStorageOptions> options, PresignedUrlSigner signer, ILogger<LocalFileStore> logger)
    {
        _options = options.Value;
        _signer = signer;
        _logger = logger;
        Directory.CreateDirectory(_options.Local.RootPath);
    }

    public async Task<FileStorageResult> UploadAsync(string container, string objectKey, Stream content, FileMetadata metadata, CancellationToken cancellationToken = default)
    {
        var (filePath, metaPath) = ResolvePaths(container, objectKey);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        var tmp = filePath + ".tmp";
        long size;
        await using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await content.CopyToAsync(fs, cancellationToken);
            size = fs.Length;
        }
        // atomic-ish replace
        if (File.Exists(filePath)) File.Delete(filePath);
        File.Move(tmp, filePath);

        var meta = new SidecarMeta(
            metadata.ContentType,
            metadata.OriginalFileName,
            metadata.CustomMetadata is null ? null : new Dictionary<string, string>(metadata.CustomMetadata),
            DateTimeOffset.UtcNow);
        await File.WriteAllTextAsync(metaPath, JsonSerializer.Serialize(meta, JsonOpts), cancellationToken);

        var etag = ComputeEtag(filePath);
        return new FileStorageResult(container, objectKey, size, metadata.ContentType, etag);
    }

    public Task<Stream?> DownloadAsync(string container, string objectKey, CancellationToken cancellationToken = default)
    {
        var (filePath, _) = ResolvePaths(container, objectKey);
        if (!File.Exists(filePath)) return Task.FromResult<Stream?>(null);
        Stream s = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult<Stream?>(s);
    }

    public async Task<FileStorageInfo?> GetInfoAsync(string container, string objectKey, CancellationToken cancellationToken = default)
    {
        var (filePath, metaPath) = ResolvePaths(container, objectKey);
        if (!File.Exists(filePath)) return null;
        var fi = new FileInfo(filePath);
        SidecarMeta? meta = null;
        if (File.Exists(metaPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(metaPath, cancellationToken);
                meta = JsonSerializer.Deserialize<SidecarMeta>(json, JsonOpts);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to read sidecar {Meta}", metaPath); }
        }
        return new FileStorageInfo(
            container, objectKey,
            fi.Length,
            meta?.ContentType ?? "application/octet-stream",
            meta?.CreatedAtUtc ?? fi.CreationTimeUtc,
            fi.LastWriteTimeUtc,
            ComputeEtag(filePath),
            meta?.CustomMetadata is null ? null : new ReadOnlyDictionary<string, string>(meta.CustomMetadata));
    }

    public Task<bool> ExistsAsync(string container, string objectKey, CancellationToken cancellationToken = default)
    {
        var (filePath, _) = ResolvePaths(container, objectKey);
        return Task.FromResult(File.Exists(filePath));
    }

    public Task<bool> DeleteAsync(string container, string objectKey, CancellationToken cancellationToken = default)
    {
        var (filePath, metaPath) = ResolvePaths(container, objectKey);
        if (!File.Exists(filePath)) return Task.FromResult(false);
        File.Delete(filePath);
        if (File.Exists(metaPath)) File.Delete(metaPath);
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<FileStorageInfo>> ListAsync(string container, string? prefix = null, int maxResults = 200, CancellationToken cancellationToken = default)
    {
        var root = Path.Combine(_options.Local.RootPath, SafeContainer(container));
        if (!Directory.Exists(root)) return Task.FromResult<IReadOnlyList<FileStorageInfo>>([]);

        var infos = new List<FileStorageInfo>();
        var searchRoot = string.IsNullOrEmpty(prefix) ? root : Path.Combine(root, prefix);
        if (!Directory.Exists(Path.GetDirectoryName(searchRoot) ?? root)) return Task.FromResult<IReadOnlyList<FileStorageInfo>>([]);

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            if (file.EndsWith(".meta.json", StringComparison.Ordinal) || file.EndsWith(".tmp", StringComparison.Ordinal)) continue;
            var relative = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
            if (!string.IsNullOrEmpty(prefix) && !relative.StartsWith(prefix, StringComparison.Ordinal)) continue;

            var fi = new FileInfo(file);
            infos.Add(new FileStorageInfo(container, relative, fi.Length, "application/octet-stream", fi.CreationTimeUtc, fi.LastWriteTimeUtc, ComputeEtag(file), null));
            if (infos.Count >= maxResults) break;
        }
        return Task.FromResult<IReadOnlyList<FileStorageInfo>>(infos);
    }

    public Task<Uri> GetPresignedUrlAsync(string container, string objectKey, TimeSpan expiry, PresignedUrlPurpose purpose, CancellationToken cancellationToken = default)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(expiry).ToUnixTimeSeconds();
        var signature = _signer.Sign(purpose, container, objectKey, expiresAt);
        var baseUrl = _options.Local.BaseUrl.TrimEnd('/');
        var prefix = _options.Local.RoutePrefix.TrimEnd('/');
        var url = $"{baseUrl}{prefix}/{Uri.EscapeDataString(container)}/{Uri.EscapeDataString(objectKey)}?expires={expiresAt}&purpose={(int)purpose}&sig={signature}";
        return Task.FromResult(new Uri(url));
    }

    public async Task<PresignedUploadTicket> CreateUploadTicketAsync(string container, string objectKey, TimeSpan expiry, FileMetadata metadata, CancellationToken cancellationToken = default)
    {
        var url = await GetPresignedUrlAsync(container, objectKey, expiry, PresignedUrlPurpose.Write, cancellationToken);
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = metadata.ContentType
        };
        if (!string.IsNullOrEmpty(metadata.OriginalFileName))
            headers["X-Original-Filename"] = metadata.OriginalFileName;
        return new PresignedUploadTicket(url, "PUT", new ReadOnlyDictionary<string, string>(headers),
            DateTimeOffset.UtcNow.Add(expiry));
    }

    public Uri? GetPublicUrl(string container, string objectKey)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl)) return null;
        var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
        return new Uri($"{baseUrl}/{Uri.EscapeDataString(container)}/{Uri.EscapeDataString(objectKey)}");
    }

    private (string FilePath, string MetaPath) ResolvePaths(string container, string objectKey)
    {
        var safeContainer = SafeContainer(container);
        var safeKey = SafeObjectKey(objectKey);
        var filePath = Path.Combine(_options.Local.RootPath, safeContainer, safeKey);
        return (filePath, filePath + ".meta.json");
    }

    private static string SafeContainer(string container)
    {
        if (string.IsNullOrWhiteSpace(container)) throw new ArgumentException("Container required.", nameof(container));
        foreach (var c in Path.GetInvalidFileNameChars())
            if (container.Contains(c)) throw new ArgumentException($"Container contains invalid character '{c}'.", nameof(container));
        return container;
    }

    private static string SafeObjectKey(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey)) throw new ArgumentException("Object key required.", nameof(objectKey));
        if (objectKey.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(objectKey))
            throw new ArgumentException("Object key must be relative and may not contain '..'.", nameof(objectKey));
        return objectKey.Replace('/', Path.DirectorySeparatorChar);
    }

    private static string ComputeEtag(string filePath)
    {
        var fi = new FileInfo(filePath);
        return $"\"{fi.Length:x}-{fi.LastWriteTimeUtc.Ticks:x}\"";
    }

    private sealed record SidecarMeta(
        string ContentType,
        string? OriginalFileName,
        Dictionary<string, string>? CustomMetadata,
        DateTimeOffset CreatedAtUtc);
}
