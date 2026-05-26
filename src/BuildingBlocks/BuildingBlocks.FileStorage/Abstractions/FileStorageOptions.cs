using System.Collections.ObjectModel;

namespace BuildingBlocks.FileStorage.Abstractions;

/// <summary>
/// Root configuration. Bound from the "FileStorage" config section.
/// </summary>
public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Provider: <c>Local</c> | <c>S3</c> | <c>AzureBlob</c>.</summary>
    public string Provider { get; set; } = "Local";

    /// <summary>Hard upper bound on file size in bytes. 0 = no limit.</summary>
    public long MaxFileSizeBytes { get; set; } = 50L * 1024 * 1024;

    /// <summary>
    /// Comma-/list-bound allow list of content-types. Empty list = allow all.
    /// Wildcards supported: "image/*", "application/pdf".
    /// </summary>
    public Collection<string> AllowedContentTypes { get; } = [];

    /// <summary>When true, every upload is streamed through the registered IFileScanner first.</summary>
    public bool VirusScanEnabled { get; set; }

    /// <summary>HMAC key used to sign local presigned URLs. REQUIRED in production for Provider=Local.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Optional public base URL for serving public objects (CDN). Empty means private only.</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    public LocalFileStorageOptions Local { get; set; } = new();
    public S3FileStorageOptions S3 { get; set; } = new();
    public AzureBlobFileStorageOptions AzureBlob { get; set; } = new();
    public ClamAvOptions ClamAv { get; set; } = new();
}

public sealed class LocalFileStorageOptions
{
    /// <summary>Root directory containing all containers. Created on startup if missing.</summary>
    public string RootPath { get; set; } = "App_Data/files";

    /// <summary>Base URL the host is reachable at — used to build presigned URLs.</summary>
    public string BaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>Mounted route prefix that the file endpoints listen on.</summary>
    public string RoutePrefix { get; set; } = "/api/v1/files";
}

public sealed class S3FileStorageOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketPrefix { get; set; } = "emm-";
    public bool ForcePathStyle { get; set; }
}

public sealed class AzureBlobFileStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerPrefix { get; set; } = "emm-";
}

public sealed class ClamAvOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 3310;
    public int TimeoutMs { get; set; } = 15_000;
}
