using Microsoft.Extensions.Options;
using TaskTracker.Application.Options;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;
    private readonly NotificationOptions _notificationOptions;

    public NotificationRepository(AppDbContext context, IOptions<NotificationOptions> notificationOptions)
    {
        _context = context;
        _notificationOptions = notificationOptions.Value;
    }

    public async Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken ct = default)
    {
        await _context.Notifications.AddRangeAsync(notifications, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<List<Notification>> GetByUserIdAsync(string userId, int take = 50, CancellationToken ct = default)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, string userId, CancellationToken ct = default)
    {
        var rows = await _context.Notifications
            .Where(n => n.Id == notificationId && n.RecipientUserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);

        return rows > 0;
    }

    public async Task MarkAllAsReadAsync(string userId, CancellationToken ct = default)
    {
        await _context.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }

    public Task<bool> ExistsForTaskTypeSinceAsync(string type, int taskId, DateTime sinceUtc, CancellationToken ct = default)
    {
        return _context.Notifications
            .AsNoTracking()
            .AnyAsync(n =>
                n.Type == type &&
                n.TaskId == taskId &&
                n.CreatedAt >= sinceUtc, ct);
    }

    public async Task PruneAsync(string userId, CancellationToken ct = default)
    {
        var keepCount = _notificationOptions.RetentionCountPerUser;

        // Find the cutoff: the Nth newest notification's CreatedAt
        var cutoff = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(keepCount)
            .Select(n => n.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (cutoff == default) return; // fewer than keepCount notifications

        await _context.Notifications
            .Where(n => n.RecipientUserId == userId && n.CreatedAt <= cutoff)
            .ExecuteDeleteAsync(ct);
    }
}
