using TaskTracker.Domain.Entities;

namespace TaskTracker.Domain.Interfaces;
// Abstracts notification persistence so Application handlers never resolve DbContext directly.
public interface INotificationRepository
{
    /// Persists multiple notifications in a single batch.
    Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken ct = default);

    /// Returns the latest notifications for a user, sorted newest first.
    Task<List<Notification>> GetByUserIdAsync(string userId, int take = 50, CancellationToken ct = default);

    /// Marks a specific notification as read (only if it belongs to the user).
    Task<bool> MarkAsReadAsync(Guid notificationId, string userId, CancellationToken ct = default);

    /// Marks all notifications for a user as read.
    Task MarkAllAsReadAsync(string userId, CancellationToken ct = default);

    /// Returns true when a matching notification already exists since the provided UTC time.
    Task<bool> ExistsForTaskTypeSinceAsync(string type, int taskId, DateTime sinceUtc, CancellationToken ct = default);

    /// Deletes old notifications, keeping only the latest N per user.
    Task PruneAsync(string userId, CancellationToken ct = default);
}
