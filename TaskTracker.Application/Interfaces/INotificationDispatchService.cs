using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Interfaces;

/// Persists notifications and dispatches matching real-time payloads.
public interface INotificationDispatchService
{
    Task DispatchAsync(IReadOnlyCollection<Notification> notifications, CancellationToken cancellationToken = default);
}
