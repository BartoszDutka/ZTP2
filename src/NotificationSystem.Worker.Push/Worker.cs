using NotificationSystem.Domain.Enums;
using NotificationSystem.Domain.Interfaces;
using NotificationSystem.Shared.Models;
using NotificationSystem.Shared.Services;

namespace NotificationSystem.Worker.Push;

public class Worker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationSender _sender;
    private readonly ILogger<Worker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(3);
    private const int MaxRetries = 3;

    public Worker(
        IServiceProvider serviceProvider,
        INotificationSender sender,
        ILogger<Worker> logger)
    {
        _serviceProvider = serviceProvider;
        _sender = sender;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Push Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNotificationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing push notifications");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("Push Worker stopped");
    }

    private async Task ProcessNotificationsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

        var notifications = await repository.GetReadyToSendAsync(DateTime.UtcNow);
        var pushNotifications = notifications.Where(n => n.Channel == NotificationChannel.Push);

        foreach (var notification in pushNotifications)
        {
            notification.Status = NotificationStatus.Sending;
            await repository.UpdateAsync(notification);

            var message = new NotificationMessage
            {
                Id = notification.Id,
                Content = notification.Content,
                Channel = notification.Channel.ToString(),
                Recipient = notification.Recipient,
                TimeZone = notification.TimeZone,
                RetryCount = notification.RetryCount
            };

            var result = await _sender.SendAsync(message);

            if (result.Success)
            {
                notification.Status = NotificationStatus.Sent;
                notification.SentAt = DateTime.UtcNow;
                notification.LastError = null;
                _logger.LogInformation("Push notification {Id} sent successfully", notification.Id);
            }
            else
            {
                notification.RetryCount++;
                notification.LastError = result.ErrorMessage;

                if (notification.RetryCount >= MaxRetries)
                {
                    notification.Status = NotificationStatus.Failed;
                    _logger.LogWarning("Push notification {Id} failed after {MaxRetries} attempts", notification.Id, MaxRetries);
                }
                else
                {
                    notification.Status = NotificationStatus.Scheduled;
                    _logger.LogInformation("Push notification {Id} will retry (attempt {Attempt})", notification.Id, notification.RetryCount + 1);
                }
            }

            await repository.UpdateAsync(notification);
        }
    }
}
