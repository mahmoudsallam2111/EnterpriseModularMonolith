using BuildingBlocks.FileStorage.Abstractions;

namespace BuildingBlocks.FileStorage.Scanning;

/// <summary>
/// Default scanner — does nothing and returns Clean. Replace via DI when the
/// FileStorage:VirusScanEnabled flag is true to plug in <c>ClamAvFileScanner</c>
/// or your provider's scan API.
/// </summary>
public sealed class NoOpFileScanner : IFileScanner
{
    public Task<FileScanResult> ScanAsync(Stream content, string objectKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(FileScanResult.Clean("NoOp"));
}
