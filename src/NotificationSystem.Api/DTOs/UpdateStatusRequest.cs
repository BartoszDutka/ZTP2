using System.ComponentModel.DataAnnotations;

namespace NotificationSystem.Api.DTOs;

public class UpdateStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
    
    public DateTime? SentAt { get; set; }
    
    public string? LastError { get; set; }
    
    public int? RetryCount { get; set; }
}
