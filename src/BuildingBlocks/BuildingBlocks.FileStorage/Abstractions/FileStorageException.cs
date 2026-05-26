namespace BuildingBlocks.FileStorage.Abstractions;

/// <summary>Thrown for invariant violations from the file storage layer (size, content type, scan reject).</summary>
public class FileStorageException : Exception
{
    public string Code { get; }
    public FileStorageException(string code, string message) : base(message) => Code = code;
    public FileStorageException(string code, string message, Exception inner) : base(message, inner) => Code = code;
}

public sealed class FileNotFoundInStoreException : FileStorageException
{
    public FileNotFoundInStoreException(string container, string objectKey)
        : base("FileStorage.NotFound", $"Object '{container}/{objectKey}' not found.") { }
}

public sealed class FileTooLargeException : FileStorageException
{
    public FileTooLargeException(long size, long max)
        : base("FileStorage.TooLarge", $"File size {size} bytes exceeds maximum {max} bytes.") { }
}

public sealed class DisallowedContentTypeException : FileStorageException
{
    public DisallowedContentTypeException(string contentType)
        : base("FileStorage.DisallowedContentType", $"Content type '{contentType}' is not allowed.") { }
}

public sealed class FileScanRejectedException : FileStorageException
{
    public FileScanRejectedException(string? threat)
        : base("FileStorage.ScanRejected", threat is null ? "Scanner rejected file." : $"Scanner found threat: {threat}") { }
}
