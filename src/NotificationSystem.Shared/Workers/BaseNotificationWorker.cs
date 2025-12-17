using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationSystem.Domain.Enums;
using NotificationSystem.Domain.Interfaces;
using NotificationSystem.Shared.Models;
using NotificationSystem.Shared.Queue;
using NotificationSystem.Shared.Services;

namespace NotificationSystem.Shared.Workers;

/// <summary>
/// Bazowy worker do przetwarzania powiadomień z danego kanału.
/// Obsługuje retry logic (max 3 próby).
/// </summary>
public abstract class BaseNotificationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationQueue _queue;
    private readonly INotificationSender _sender;
    private readonly ILogger _logger;
    private readonly string _channelName;
    private const int MaxRetries = 3;

    protected BaseNotificationWorker(
        IServiceProvider serviceProvider,
        INotificationQueue queue,
        INotificationSender sender,
        ILogger logger,
        string channelName)
    {
        _serviceProvider = serviceProvider;
        _queue = queue;
        _sender = sender;
        _logger = logger;
        _channelName = channelName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Channel} Worker started", _channelName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _queue.DequeueAsync(_channelName, stoppingToken);
                
                if (message != null)
                {
                    await ProcessMessageAsync(message);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Channel} worker", _channelName);
            }
        }

        _logger.LogInformation("{Channel} Worker stopped", _channelName);
    }

    private async Task ProcessMessageAsync(NotificationMessage message)
    {
        _logger.LogInformation("Processing notification {Id} (attempt {Attempt}/{MaxRetries})", 
            message.Id, message.RetryCount + 1, MaxRetries);

        var result = await _sender.SendAsync(message);

        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        
        var notification = await repository.GetByIdAsync(message.Id);
        
        if (notification == null)
        {
            _logger.LogWarning("Notification {Id} not found in database", message.Id);
            return;
        }

        if (result.Success)
        {
            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTime.UtcNow;
            notification.LastError = null;
            
            _logger.LogInformation("Notification {Id} sent successfully", message.Id);
        }
        else
        {
            notification.RetryCount++;
            notification.LastError = result.ErrorMessage;

            if (notification.RetryCount >= MaxRetries)
            {
                notification.Status = NotificationStatus.Failed;
                _logger.LogWarning("Notification {Id} failed after {MaxRetries} attempts", 
                    message.Id, MaxRetries);
            }
            else
            {
                // Ponowne dodanie do kolejki dla retry
                notification.Status = NotificationStatus.Scheduled;
                
                var retryMessage = new NotificationMessage
                {
                    Id = notification.Id,
                    Content = notification.Content,
                    Channel = notification.Channel.ToString(),
                    Recipient = notification.Recipient,
                    TimeZone = notification.TimeZone,
                    RetryCount = notification.RetryCount
                };

                await _queue.EnqueueAsync(retryMessage, _channelName);
                
                _logger.LogInformation("Notification {Id} requeued for retry (attempt {Attempt})", 
                    message.Id, notification.RetryCount + 1);
            }
        }

        await repository.UpdateAsync(notification);
    }
}
