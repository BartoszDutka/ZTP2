namespace NotificationSystem.Shared.Models;

public class SendResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public static SendResult Successful() => new() { Success = true };
    public static SendResult Failure(string error) => new() { Success = false, ErrorMessage = error };
}
