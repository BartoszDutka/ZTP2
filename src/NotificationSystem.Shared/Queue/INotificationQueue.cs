using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Queue;

public interface INotificationQueue
{
    Task EnqueueAsync(NotificationMessage message, string channel);
    Task<NotificationMessage?> DequeueAsync(string channel, CancellationToken cancellationToken);
}
