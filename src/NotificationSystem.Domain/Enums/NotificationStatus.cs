namespace NotificationSystem.Domain.Enums;

public enum NotificationStatus
{
    Pending = 0,
    Scheduled = 1,
    Sending = 2,
    Sent = 3,
    Failed = 4,
    Cancelled = 5
}
