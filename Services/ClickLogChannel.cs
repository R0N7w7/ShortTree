using System.Threading.Channels;

namespace ShortTree.Services
{
    public sealed class ClickLogChannel
    {
        public ClickLogChannel()
        {
            Channel = System.Threading.Channels.Channel.CreateUnbounded<ClickLogEntry>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
            );
        }

        public Channel<ClickLogEntry> Channel { get; }
    }

    public sealed record ClickLogEntry(
        int LinkId,
        DateTime Timestamp,
        string? IpAddress,
        string? UserAgent,
        string? Referer
    );
}
