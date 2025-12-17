using Microsoft.Extensions.Logging;
using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Services;

/// <summary>
/// Symulator wysyłki powiadomień. 
/// Około 30% prób kończy się losowym niepowodzeniem.
/// </summary>
public class NotificationSenderSimulator : INotificationSender
{
    private readonly ILogger<NotificationSenderSimulator> _logger;
    private readonly Random _random = new();
    private const double FailureRate = 0.3; // 30% failure rate

    public NotificationSenderSimulator(ILogger<NotificationSenderSimulator> logger)
    {
        _logger = logger;
    }

    public async Task<SendResult> SendAsync(NotificationMessage message)
    {
        // Symulacja opóźnienia wysyłki
        await Task.Delay(100);

        // Losowe niepowodzenie z 30% prawdopodobieństwem
        if (_random.NextDouble() < FailureRate)
        {
            var error = $"Simulated failure for notification {message.Id}";
            _logger.LogWarning("❌ [{Channel}] Failed to send notification {Id} to {Recipient}: {Error}",
                message.Channel, message.Id, message.Recipient, error);
            
            return SendResult.Failure(error);
        }

        // Sukces - zapis do logu zamiast realnej wysyłki
        _logger.LogInformation("✅ [{Channel}] Successfully sent notification {Id} to {Recipient}: {Content}",
            message.Channel, message.Id, message.Recipient, message.Content);

        return SendResult.Successful();
    }
}
