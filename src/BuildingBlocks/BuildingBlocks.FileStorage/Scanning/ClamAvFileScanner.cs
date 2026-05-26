using System.Net.Sockets;
using System.Text;
using BuildingBlocks.FileStorage.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.FileStorage.Scanning;

/// <summary>
/// Talks INSTREAM to a ClamAV daemon (clamd) over TCP. Reasonable default for
/// production. The connection is one-shot per scan; ClamAV is happy with that and
/// it avoids long-lived connection management here.
/// </summary>
public sealed class ClamAvFileScanner : IFileScanner
{
    private readonly ClamAvOptions _options;
    private readonly ILogger<ClamAvFileScanner> _logger;

    public ClamAvFileScanner(IOptions<FileStorageOptions> options, ILogger<ClamAvFileScanner> logger)
    {
        _options = options.Value.ClamAv;
        _logger = logger;
    }

    public async Task<FileScanResult> ScanAsync(Stream content, string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new TcpClient();
            client.SendTimeout = _options.TimeoutMs;
            client.ReceiveTimeout = _options.TimeoutMs;
            await client.ConnectAsync(_options.Host, _options.Port, cancellationToken);

            using var stream = client.GetStream();
            var command = Encoding.ASCII.GetBytes("zINSTREAM\0");
            await stream.WriteAsync(command, cancellationToken);

            var chunk = new byte[64 * 1024];
            int read;
            while ((read = await content.ReadAsync(chunk, cancellationToken)) > 0)
            {
                var sizeBytes = BitConverter.GetBytes((uint)read);
                if (BitConverter.IsLittleEndian) Array.Reverse(sizeBytes);
                await stream.WriteAsync(sizeBytes, cancellationToken);
                await stream.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
            }
            // zero-length chunk terminator
            await stream.WriteAsync(new byte[] { 0, 0, 0, 0 }, cancellationToken);
            await stream.FlushAsync(cancellationToken);

            var responseBuffer = new byte[1024];
            var n = await stream.ReadAsync(responseBuffer, cancellationToken);
            var response = Encoding.ASCII.GetString(responseBuffer, 0, n).TrimEnd('\0', '\n', ' ');

            if (response.EndsWith("OK", StringComparison.Ordinal))
                return FileScanResult.Clean("ClamAV");

            // "stream: Eicar-Signature FOUND"
            var marker = response.IndexOf("FOUND", StringComparison.Ordinal);
            var threat = marker > 0 ? response[..marker].Trim().TrimEnd(':').Trim() : "unknown";
            return FileScanResult.Infected(threat, "ClamAV");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ClamAV scan failed for {ObjectKey}; treating as INFECTED to fail closed.", objectKey);
            return FileScanResult.Infected("scanner-error", "ClamAV");
        }
    }
}
