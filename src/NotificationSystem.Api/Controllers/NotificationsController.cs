using Microsoft.AspNetCore.Mvc;
using NotificationSystem.Api.DTOs;
using NotificationSystem.Domain.Entities;
using NotificationSystem.Domain.Enums;
using NotificationSystem.Domain.Interfaces;

namespace NotificationSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationRepository repository,
        ILogger<NotificationsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetAll()
    {
        var notifications = await _repository.GetAllAsync();
        return Ok(notifications.Select(MapToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NotificationResponse>> GetById(Guid id)
    {
        var notification = await _repository.GetByIdAsync(id);
        
        if (notification == null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(notification));
    }

    [HttpPost]
    public async Task<ActionResult<NotificationResponse>> Create([FromBody] CreateNotificationRequest request)
    {
        if (!Enum.TryParse<NotificationChannel>(request.Channel, true, out var channel))
        {
            return BadRequest($"Invalid channel. Allowed values: {string.Join(", ", Enum.GetNames<NotificationChannel>())}");
        }

        if (request.ScheduledAt <= DateTime.UtcNow)
        {
            return BadRequest("ScheduledAt must be in the future");
        }

        var notification = new Notification
        {
            Content = request.Content,
            Channel = channel,
            Recipient = request.Recipient,
            TimeZone = request.TimeZone,
            ScheduledAt = request.ScheduledAt,
            Priority = request.Priority
        };

        var created = await _repository.CreateAsync(notification);
        
        _logger.LogInformation("Created notification {Id} scheduled for {ScheduledAt}", 
            created.Id, created.ScheduledAt);

        return CreatedAtAction(
            nameof(GetById), 
            new { id = created.Id }, 
            MapToResponse(created));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<NotificationResponse>> Cancel(Guid id)
    {
        var notification = await _repository.GetByIdAsync(id);
        
        if (notification == null)
        {
            return NotFound();
        }

        if (notification.Status == NotificationStatus.Sent)
        {
            return BadRequest("Cannot cancel a notification that has already been sent");
        }

        if (notification.Status == NotificationStatus.Cancelled)
        {
            return BadRequest("Notification is already cancelled");
        }

        notification.Status = NotificationStatus.Cancelled;
        await _repository.UpdateAsync(notification);

        _logger.LogInformation("Cancelled notification {Id}", id);

        return Ok(MapToResponse(notification));
    }

    [HttpPost("{id:guid}/send-now")]
    public async Task<ActionResult<NotificationResponse>> SendNow(Guid id)
    {
        var notification = await _repository.GetByIdAsync(id);
        
        if (notification == null)
        {
            return NotFound();
        }

        if (notification.Status == NotificationStatus.Sent)
        {
            return BadRequest("Notification has already been sent");
        }

        if (notification.Status == NotificationStatus.Cancelled)
        {
            return BadRequest("Cannot send a cancelled notification");
        }

        // Zmiana scheduledAt na teraz - workery pobiorą to powiadomienie
        notification.ScheduledAt = DateTime.UtcNow;
        notification.Status = NotificationStatus.Scheduled;
        await _repository.UpdateAsync(notification);

        _logger.LogInformation("Triggered immediate send for notification {Id}", id);

        return Ok(MapToResponse(notification));
    }

    // Endpointy dla workerów (zewnętrznych dostawców)
    
    [HttpGet("ready/{channel}")]
    public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetReadyToSend(string channel)
    {
        if (!Enum.TryParse<NotificationChannel>(channel, true, out var notificationChannel))
        {
            return BadRequest($"Invalid channel. Allowed values: {string.Join(", ", Enum.GetNames<NotificationChannel>())}");
        }

        var notifications = await _repository.GetReadyToSendAsync(DateTime.UtcNow);
        var filtered = notifications
            .Where(n => n.Channel == notificationChannel)
            .Select(MapToResponse);
        
        return Ok(filtered);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<NotificationResponse>> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var notification = await _repository.GetByIdAsync(id);
        
        if (notification == null)
        {
            return NotFound();
        }

        if (!Enum.TryParse<NotificationStatus>(request.Status, true, out var status))
        {
            return BadRequest($"Invalid status. Allowed values: {string.Join(", ", Enum.GetNames<NotificationStatus>())}");
        }

        // Walidacja przejść między stanami
        if (!IsValidStatusTransition(notification.Status, status))
        {
            return BadRequest($"Invalid status transition from {notification.Status} to {status}");
        }

        notification.Status = status;
        
        if (request.SentAt.HasValue)
        {
            notification.SentAt = request.SentAt.Value;
        }
        
        if (request.LastError != null)
        {
            notification.LastError = request.LastError;
        }
        
        if (request.RetryCount.HasValue)
        {
            notification.RetryCount = request.RetryCount.Value;
        }

        await _repository.UpdateAsync(notification);

        return Ok(MapToResponse(notification));
    }

    private static bool IsValidStatusTransition(NotificationStatus currentStatus, NotificationStatus newStatus)
    {
        // Dozwolone przejścia:
        // Pending/Scheduled -> Sending (worker zaczyna przetwarzanie)
        // Pending/Scheduled -> Cancelled (użytkownik anuluje)
        // Sending -> Sent (sukces)
        // Sending -> Failed (po wyczerpaniu prób)
        // Sending -> Scheduled (retry - powrót do kolejki)
        // Failed -> Scheduled (ponowna próba przez użytkownika)
        
        return (currentStatus, newStatus) switch
        {
            (NotificationStatus.Pending, NotificationStatus.Scheduled) => true,
            (NotificationStatus.Pending, NotificationStatus.Cancelled) => true,
            (NotificationStatus.Scheduled, NotificationStatus.Sending) => true,
            (NotificationStatus.Scheduled, NotificationStatus.Cancelled) => true,
            (NotificationStatus.Sending, NotificationStatus.Sent) => true,
            (NotificationStatus.Sending, NotificationStatus.Failed) => true,
            (NotificationStatus.Sending, NotificationStatus.Scheduled) => true, // retry
            (NotificationStatus.Failed, NotificationStatus.Scheduled) => true, // ponowna próba
            _ => false
        };
    }

    private static NotificationResponse MapToResponse(Notification notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            Content = notification.Content,
            Channel = notification.Channel.ToString(),
            Recipient = notification.Recipient,
            TimeZone = notification.TimeZone,
            ScheduledAt = notification.ScheduledAt,
            Priority = notification.Priority,
            Status = notification.Status.ToString(),
            RetryCount = notification.RetryCount,
            CreatedAt = notification.CreatedAt,
            SentAt = notification.SentAt,
            LastError = notification.LastError
        };
    }
}
