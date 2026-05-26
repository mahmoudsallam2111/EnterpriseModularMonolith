using System.Collections.ObjectModel;
using BuildingBlocks.FileStorage.Abstractions;
using BuildingBlocks.FileStorage.Hashing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.FileStorage.Local;

/// <summary>
/// Disk-backed file store. Each container becomes a subdirectory of
/// <see cref="LocalFileStorageOptions.RootPath"/>; each object becomes a regular file.
/// Presigned URLs are HMAC-signed query strings that the FileEndpoints honour.
/// </summary>
public sealed class LocalFileStore : IFileStore
{
    private readonly FileStorageOptions _options;
    private readonly PresignedUrlSigner _signer;
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    public LocalFileStore(IOptions<FileStorageOptions> options, PresignedUrlSigner signer)
    {
        _options = options.Value;
        _signer = signer;
        Directory.CreateDirectory(_options.Local.RootPath);
    }

    public async Task<FileStorageResult> UploadAsync(string container, string objectKey, Stream content, FileMetadata metadata, CancellationToken cancellationToken = default)
    {
        var filePath = ResolvePath(container, objectKey);
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

        var legacyMetaPath = GetLegacyMetaPath(filePath);
        if (File.Exists(legacyMetaPath))
            File.Delete(legacyMetaPath);

        var etag = ComputeEtag(filePath);
        return new FileStorageResult(container, objectKey, size, metadata.ContentType, etag);
    }

    public Task<Stream?> DownloadAsync(string container, string objectKey, CancellationToken cancellationToken = default)
    {
        var filePath = ResolvePath(container, objectKey);
        if (!File.Exists(filePath)) return Task.FromResult<Stream?>(null);
        Stream s = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult<Stream?>(s);
    }

    public Task<FileStorageInfo?> GetInfoAsync(string container, string objectKey, CancellationToken cancellationToken = default)
    {
        var filePath = ResolvePath(container, objectKey);
        if (!File.Exists(filePath)) return Task.FromResult<FileStorageInfo?>(null);
        var fi = new FileInfo(filePath);

        FileStorageInfo? info = new(
            container, objectKey,
            fi.Length,
            InferContentType(objectKey),
            fi.CreationTimeUtc,
            fi.LastWriteTimeUtc,
            ComputeEtag(filePath),
            null);

        return Task.FromResult<FileStorageInfo?>(info);
    }

    public Task<bool> ExistsAsync(string container, string objectKey, CancellationToken cancellationToken = default)
    {
        var filePath = ResolvePath(container, objectKey);
        return Task.FromResult(File.Exists(filePath));
    }

    public Task<bool> DeleteAsync(string container, string objectKey, CancellationToken cancellationToken = default)
    {
        var filePath = ResolvePath(container, objectKey);
        if (!File.Exists(filePath)) return Task.FromResult(false);
        File.Delete(filePath);
        var legacyMetaPath = GetLegacyMetaPath(filePath);
        if (File.Exists(legacyMetaPath)) File.Delete(legacyMetaPath);
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
            infos.Add(new FileStorageInfo(container, relative, fi.Length, InferContentType(relative), fi.CreationTimeUtc, fi.LastWriteTimeUtc, ComputeEtag(file), null));
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

    private string ResolvePath(string container, string objectKey)
    {
        var safeContainer = SafeContainer(container);
        var safeKey = SafeObjectKey(objectKey);
        return Path.Combine(_options.Local.RootPath, safeContainer, safeKey);
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

    private static string InferContentType(string objectKey) =>
        ContentTypes.TryGetContentType(objectKey, out var contentType)
            ? contentType
            : "application/octet-stream";

    private static string GetLegacyMetaPath(string filePath) => filePath + ".meta.json";
}
