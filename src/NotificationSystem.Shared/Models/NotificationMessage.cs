namespace NotificationSystem.Shared.Models;

public class NotificationMessage
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "UTC";
    public int RetryCount { get; set; }
}
