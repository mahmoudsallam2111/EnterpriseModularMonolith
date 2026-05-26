using BuildingBlocks.FileStorage.Abstractions;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.FileStorage.Pipeline;

/// <summary>
/// Enforces FileStorageOptions.AllowedContentTypes. Wildcards supported:
/// "image/*" matches "image/png", "image/jpeg", etc. Empty allow-list = allow all.
/// </summary>
public sealed class ContentTypeAllowListValidator : IFileValidator
{
    private readonly FileStorageOptions _options;
    public ContentTypeAllowListValidator(IOptions<FileStorageOptions> options) => _options = options.Value;

    public Task<FileValidationResult> ValidateAsync(FileUploadCandidate candidate, CancellationToken cancellationToken = default)
    {
        if (_options.AllowedContentTypes.Count == 0)
            return Task.FromResult(FileValidationResult.Valid());

        var ct = candidate.Metadata.ContentType ?? string.Empty;
        foreach (var allowed in _options.AllowedContentTypes)
        {
            if (Matches(allowed, ct))
                return Task.FromResult(FileValidationResult.Valid());
        }
        return Task.FromResult(FileValidationResult.Invalid(
            "FileStorage.DisallowedContentType",
            $"Content type '{ct}' is not allowed."));
    }

    private static bool Matches(string allowed, string contentType)
    {
        if (allowed.EndsWith("/*", StringComparison.Ordinal))
        {
            var prefix = allowed[..^2];
            return contentType.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
        }
        return string.Equals(allowed, contentType, StringComparison.OrdinalIgnoreCase);
    }
}
