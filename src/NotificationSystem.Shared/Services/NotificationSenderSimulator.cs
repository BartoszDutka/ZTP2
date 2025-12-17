using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Services;

/// <summary>
/// Symulator wysyłki powiadomień z gwarancją idempotentności.
/// Około 30% prób kończy się losowym niepowodzeniem.
/// Przechowuje w pamięci zbiór przetworzonych identyfikatorów - każda wiadomość może być wysłana tylko raz.
/// </summary>
public class NotificationSenderSimulator : INotificationSender
{
    private readonly ILogger<NotificationSenderSimulator> _logger;
    private readonly Random _random = new();
    private readonly ConcurrentDictionary<Guid, DateTime> _sentMessages = new();
    private const double FailureRate = 0.3; // 30% failure rate

    public NotificationSenderSimulator(ILogger<NotificationSenderSimulator> logger)
    {
        _logger = logger;
    }

    public async Task<SendResult> SendAsync(NotificationMessage message)
    {
        // Sprawdzenie idempotentności - czy wiadomość już została wysłana
        if (_sentMessages.TryGetValue(message.Id, out var sentAt))
        {
            _logger.LogInformation("⚠️ [{Channel}] Notification {Id} already delivered at {SentAt} - skipping duplicate",
                message.Channel, message.Id, sentAt);
            
            return SendResult.AlreadySent(sentAt);
        }

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

        // Rejestracja wysłania (gwarancja idempotentności)
        var now = DateTime.UtcNow;
        if (!_sentMessages.TryAdd(message.Id, now))
        {
            // Inny wątek zdążył wysłać - zwróć informację o duplikacie
            _logger.LogInformation("⚠️ [{Channel}] Notification {Id} was sent by another worker - skipping duplicate",
                message.Channel, message.Id);
            
            return SendResult.AlreadySent(_sentMessages[message.Id]);
        }

        // Sukces - zapis do logu zamiast realnej wysyłki
        _logger.LogInformation("✅ [{Channel}] Successfully sent notification {Id} to {Recipient}: {Content}",
            message.Channel, message.Id, message.Recipient, message.Content);

        return SendResult.Successful();
    }
}
