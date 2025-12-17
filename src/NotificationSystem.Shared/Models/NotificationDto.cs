namespace NotificationSystem.Shared.Models;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "UTC";
    public DateTime ScheduledAt { get; set; }
    public int? Priority { get; set; }
    public string Status { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? LastError { get; set; }
}
