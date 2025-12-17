using NotificationSystem.Shared.Models;
using NotificationSystem.Shared.Services;

namespace NotificationSystem.Worker.Email;

public class Worker : BackgroundService
{
    private readonly INotificationApiClient _apiClient;
    private readonly INotificationSender _sender;
    private readonly IMetricsService _metrics;
    private readonly ILogger<Worker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(3);
    private const int MaxRetries = 3;

    public Worker(
        INotificationApiClient apiClient,
        INotificationSender sender,
        IMetricsService metrics,
        ILogger<Worker> logger)
    {
        _apiClient = apiClient;
        _sender = sender;
        _metrics = metrics;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNotificationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email notifications");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("Email Worker stopped");
    }

    private async Task ProcessNotificationsAsync()
    {
        var notifications = await _apiClient.GetReadyToSendAsync("Email");

        foreach (var notification in notifications)
        {
            // Sprawdź godziny ciszy (22:00-7:00 w strefie czasowej odbiorcy)
            if (IsQuietHours(notification.TimeZone))
            {
                _logger.LogInformation("Email notification {Id} delayed - quiet hours in timezone {TimeZone}", 
                    notification.Id, notification.TimeZone);
                continue;
            }

            // Zmień status na Sending
            await _apiClient.UpdateStatusAsync(notification.Id, "Sending");

            var message = new NotificationMessage
            {
                Id = notification.Id,
                Content = notification.Content,
                Channel = "Email",
                Recipient = notification.Recipient,
                TimeZone = notification.TimeZone,
                RetryCount = notification.RetryCount
            };

            var result = await _sender.SendAsync(message);

            if (result.Success)
            {
                var sentAt = result.WasAlreadySent ? result.SentAt : DateTime.UtcNow;
                await _apiClient.UpdateStatusAsync(notification.Id, "Sent", sentAt);
                
                if (!result.WasAlreadySent)
                {
                    _metrics.IncrementSentCount();
                }
                
                if (result.WasAlreadySent)
                {
                    _logger.LogInformation("Email notification {Id} was already sent - skipping duplicate", notification.Id);
                }
                else
                {
                    _logger.LogInformation("Email notification {Id} sent successfully", notification.Id);
                }
            }
            else
            {
                var newRetryCount = notification.RetryCount + 1;

                if (newRetryCount >= MaxRetries)
                {
                    await _apiClient.UpdateStatusAsync(notification.Id, "Failed", null, result.ErrorMessage, newRetryCount);
                    _metrics.IncrementFailedCount();
                    _logger.LogWarning("Email notification {Id} failed after {MaxRetries} attempts", notification.Id, MaxRetries);
                }
                else
                {
                    await _apiClient.UpdateStatusAsync(notification.Id, "Scheduled", null, result.ErrorMessage, newRetryCount);
                    _logger.LogInformation("Email notification {Id} will retry (attempt {Attempt})", notification.Id, newRetryCount + 1);
                }
            }
        }
    }

    private static bool IsQuietHours(string timeZoneId)
    {
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            var hour = localTime.Hour;
            
            // Godziny ciszy: 22:00-7:00
            return hour >= 22 || hour < 7;
        }
        catch
        {
            // Jeśli strefa czasowa jest nieznana, nie blokujemy wysyłki
            return false;
        }
    }
}
