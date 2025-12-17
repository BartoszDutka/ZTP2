using MongoDB.Driver;
using NotificationSystem.Domain.Entities;
using NotificationSystem.Domain.Enums;
using NotificationSystem.Domain.Interfaces;

namespace NotificationSystem.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly IMongoCollection<Notification> _collection;

    public NotificationRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Notification>("Notifications");
    }

    public async Task<Notification?> GetByIdAsync(Guid id)
    {
        return await _collection.Find(n => n.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Notification>> GetAllAsync()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    public async Task<IEnumerable<Notification>> GetReadyToSendAsync(DateTime currentTime)
    {
        var filter = Builders<Notification>.Filter.And(
            Builders<Notification>.Filter.Eq(n => n.Status, NotificationStatus.Scheduled),
            Builders<Notification>.Filter.Lte(n => n.ScheduledAt, currentTime)
        );
        
        var sort = Builders<Notification>.Sort
            .Ascending(n => n.ScheduledAt)
            .Ascending(n => n.Priority);
            
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        notification.Id = Guid.NewGuid();
        notification.CreatedAt = DateTime.UtcNow;
        notification.Status = NotificationStatus.Scheduled;
        notification.RetryCount = 0;

        await _collection.InsertOneAsync(notification);
        return notification;
    }

    public async Task UpdateAsync(Notification notification)
    {
        await _collection.ReplaceOneAsync(n => n.Id == notification.Id, notification);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _collection.DeleteOneAsync(n => n.Id == id);
    }

    public async Task<IEnumerable<Notification>> GetByStatusAsync(NotificationStatus status)
    {
        return await _collection.Find(n => n.Status == status).ToListAsync();
    }
}
