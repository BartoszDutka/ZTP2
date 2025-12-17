using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using NotificationSystem.Domain.Enums;

namespace NotificationSystem.Domain.Entities;

public class Notification
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    
    public string Content { get; set; } = string.Empty;
    
    [BsonRepresentation(BsonType.String)]
    public NotificationChannel Channel { get; set; }
    
    public string Recipient { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "UTC";
    public DateTime ScheduledAt { get; set; }
    public int? Priority { get; set; }
    
    [BsonRepresentation(BsonType.String)]
    public NotificationStatus Status { get; set; }
    
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? LastError { get; set; }
}
