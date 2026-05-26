namespace BuildingBlocks.FileStorage.Abstractions;

/// <summary>
/// Antivirus / antimalware hook. Called pre-upload when scanning is enabled.
/// The default <c>NoOpFileScanner</c> always returns clean; plug in
/// <c>ClamAvFileScanner</c> or your provider's scanner via DI replacement.
/// </summary>
public interface IFileScanner
{
    Task<FileScanResult> ScanAsync(Stream content, string objectKey, CancellationToken cancellationToken = default);
}

public sealed record FileScanResult(bool IsClean, string? ThreatName = null, string? Scanner = null)
{
    public static FileScanResult Clean(string? scanner = null) => new(true, null, scanner);
    public static FileScanResult Infected(string threatName, string? scanner = null) => new(false, threatName, scanner);
}
