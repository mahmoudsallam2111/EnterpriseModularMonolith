namespace BuildingBlocks.FileStorage.Abstractions;

/// <summary>
/// Pre-upload validation step. Each registered validator runs in sequence; the first
/// failure short-circuits the pipeline and the upload is rejected with a typed exception.
/// </summary>
public interface IFileValidator
{
    Task<FileValidationResult> ValidateAsync(FileUploadCandidate candidate, CancellationToken cancellationToken = default);
}

public sealed record FileUploadCandidate(
    string Container,
    string ObjectKey,
    FileMetadata Metadata,
    long SizeBytes);

public sealed record FileValidationResult(bool IsValid, string? Code, string? Reason)
{
    public static FileValidationResult Valid() => new(true, null, null);
    public static FileValidationResult Invalid(string code, string reason) => new(false, code, reason);
}
