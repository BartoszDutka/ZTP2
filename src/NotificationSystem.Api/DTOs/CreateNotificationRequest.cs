using System.ComponentModel.DataAnnotations;

namespace NotificationSystem.Api.DTOs;

public class CreateNotificationRequest
{
    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    [Required]
    public string Channel { get; set; } = string.Empty; // "Email" or "Push"

    [Required]
    [MaxLength(500)]
    public string Recipient { get; set; } = string.Empty;

    [Required]
    public string TimeZone { get; set; } = "UTC";

    [Required]
    public DateTime ScheduledAt { get; set; }

    public int? Priority { get; set; }
}
