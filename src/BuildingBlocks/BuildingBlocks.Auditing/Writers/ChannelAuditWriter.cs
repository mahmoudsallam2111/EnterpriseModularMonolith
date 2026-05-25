using System.Threading.Channels;
using BuildingBlocks.Auditing.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Auditing.Writers;

/// <summary>
/// Producer side. Enqueues scopes onto a bounded channel that the background
/// hosted service drains into the audit database. Bounded + DropOldest so a
/// stalled audit DB can never block business operations or grow memory unbounded.
/// </summary>
public sealed class ChannelAuditWriter : IAuditWriter
{
    private readonly Channel<AuditScope> _channel;
    private readonly ILogger<ChannelAuditWriter> _logger;

    internal ChannelReader<AuditScope> Reader => _channel.Reader;

    public ChannelAuditWriter(IOptions<AuditingOptions> options, ILogger<ChannelAuditWriter> logger)
    {
        _logger = logger;
        _channel = Channel.CreateBounded<AuditScope>(new BoundedChannelOptions(options.Value.ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
    }

    public ValueTask WriteAsync(AuditScope scope, CancellationToken cancellationToken = default)
    {
        if (!_channel.Writer.TryWrite(scope))
        {
            // Should not happen with DropOldest, but defensively log.
            _logger.LogWarning("Audit channel rejected scope {Action}; dropped.", scope.Action);
        }
        return ValueTask.CompletedTask;
    }
}
