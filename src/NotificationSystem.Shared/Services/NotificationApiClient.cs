using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Services;

public interface INotificationApiClient
{
    Task<IEnumerable<NotificationDto>> GetReadyToSendAsync(string channel);
    Task UpdateStatusAsync(Guid id, string status, DateTime? sentAt = null, string? lastError = null, int? retryCount = null);
}

public class NotificationApiClient : INotificationApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationApiClient> _logger;

    public NotificationApiClient(HttpClient httpClient, ILogger<NotificationApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<NotificationDto>> GetReadyToSendAsync(string channel)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<NotificationDto>>($"api/notifications/ready/{channel}");
            return response ?? Enumerable.Empty<NotificationDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching notifications for channel {Channel}", channel);
            return Enumerable.Empty<NotificationDto>();
        }
    }

    public async Task UpdateStatusAsync(Guid id, string status, DateTime? sentAt = null, string? lastError = null, int? retryCount = null)
    {
        var request = new
        {
            Status = status,
            SentAt = sentAt,
            LastError = lastError,
            RetryCount = retryCount
        };

        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/notifications/{id}/status", request);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for notification {Id}", id);
            throw;
        }
    }
}
