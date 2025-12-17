using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Services;

public interface INotificationSender
{
    Task<SendResult> SendAsync(NotificationMessage message);
}
