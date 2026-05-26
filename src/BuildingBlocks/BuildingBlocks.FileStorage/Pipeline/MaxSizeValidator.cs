using BuildingBlocks.FileStorage.Abstractions;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.FileStorage.Pipeline;

public sealed class MaxSizeValidator : IFileValidator
{
    private readonly FileStorageOptions _options;
    public MaxSizeValidator(IOptions<FileStorageOptions> options) => _options = options.Value;

    public Task<FileValidationResult> ValidateAsync(FileUploadCandidate candidate, CancellationToken cancellationToken = default)
    {
        if (_options.MaxFileSizeBytes > 0 && candidate.SizeBytes > _options.MaxFileSizeBytes)
            return Task.FromResult(FileValidationResult.Invalid(
                "FileStorage.TooLarge",
                $"File size {candidate.SizeBytes} exceeds maximum {_options.MaxFileSizeBytes}."));
        return Task.FromResult(FileValidationResult.Valid());
    }
}
