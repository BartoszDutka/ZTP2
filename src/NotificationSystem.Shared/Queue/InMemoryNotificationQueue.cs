using System.Threading.Channels;
using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Queue;

/// <summary>
/// Prosta kolejka in-memory dla powiadomień.
/// W produkcji powinna być zastąpiona przez RabbitMQ lub inny message broker.
/// </summary>
public class InMemoryNotificationQueue : INotificationQueue
{
    private readonly Dictionary<string, Channel<NotificationMessage>> _channels = new();
    private readonly object _lock = new();

    public Task EnqueueAsync(NotificationMessage message, string channel)
    {
        var ch = GetOrCreateChannel(channel);
        return ch.Writer.WriteAsync(message).AsTask();
    }

    public async Task<NotificationMessage?> DequeueAsync(string channel, CancellationToken cancellationToken)
    {
        var ch = GetOrCreateChannel(channel);
        
        try
        {
            return await ch.Reader.ReadAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private Channel<NotificationMessage> GetOrCreateChannel(string channelName)
    {
        lock (_lock)
        {
            if (!_channels.TryGetValue(channelName, out var channel))
            {
                channel = Channel.CreateUnbounded<NotificationMessage>(new UnboundedChannelOptions
                {
                    SingleReader = false,
                    SingleWriter = false
                });
                _channels[channelName] = channel;
            }
            return channel;
        }
    }
}
