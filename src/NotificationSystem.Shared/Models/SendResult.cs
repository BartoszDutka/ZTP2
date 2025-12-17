namespace NotificationSystem.Shared.Models;

public class SendResult
{
    public bool Success { get; set; }
    public bool WasAlreadySent { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }

    public static SendResult Successful() => new() { Success = true };
    public static SendResult Failure(string error) => new() { Success = false, ErrorMessage = error };
    public static SendResult AlreadySent(DateTime sentAt) => new() { Success = true, WasAlreadySent = true, SentAt = sentAt };
}
