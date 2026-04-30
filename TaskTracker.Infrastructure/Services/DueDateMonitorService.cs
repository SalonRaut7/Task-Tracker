using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Interfaces;
using TaskTracker.Application.Options;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Services;

/// Background service that runs on a configurable interval to detect tasks
/// approaching or past their due date and pushes notifications.

public class DueDateMonitorService : BackgroundService
{
    private readonly NotificationOptions _notificationOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DueDateMonitorService> _logger;

    public DueDateMonitorService(
        IServiceScopeFactory scopeFactory,
        IOptions<NotificationOptions> notificationOptions,
        ILogger<DueDateMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _notificationOptions = notificationOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(_notificationOptions.DueDateMonitorIntervalMinutes);

        _logger.LogInformation("DueDateMonitorService started. Interval: {Interval}", interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckDueDatesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DueDateMonitorService check cycle");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task CheckDueDatesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pushService = scope.ServiceProvider.GetRequiredService<INotificationPushService>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dueSoonThreshold = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(_notificationOptions.DueSoonWindowHours));

        // Find tasks that are overdue or due within the configured window, and not completed/cancelled
        var dueTasks = await dbContext.Tasks
            .AsNoTracking()
            .Include(t => t.Project)
            .Where(t => t.EndDate.HasValue
                && t.Status != Status.Completed
                && t.Status != Status.Cancelled
                && t.EndDate.Value <= dueSoonThreshold)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.ProjectId,
                t.AssigneeId,
                t.ReporterId,
                EndDate = t.EndDate!.Value,
            })
            .ToListAsync(ct);

        if (dueTasks.Count == 0) return;

        _logger.LogInformation("DueDateMonitor found {Count} tasks near/past due date", dueTasks.Count);

        foreach (var task in dueTasks)
        {
            var isOverdue = task.EndDate < today;
            var notificationType = isOverdue ? "TaskOverdue" : "TaskDueSoon";

            // Check if we already sent this notification today
            var alreadySent = await dbContext.Notifications
                .AsNoTracking()
                .AnyAsync(n => n.Type == notificationType
                    && n.TaskId == task.Id
                    && n.CreatedAt >= DateTime.UtcNow.Date, ct);

            if (alreadySent) continue;

            var taskRef = string.IsNullOrWhiteSpace(task.Title)
                ? $"TASK-{task.Id}"
                : $"{task.Title} (TASK-{task.Id})";

            var message = isOverdue
                ? $"{taskRef} is past its due date ({task.EndDate:MMM dd})"
                : $"{taskRef} is due within {_notificationOptions.DueSoonWindowHours} hours ({task.EndDate:MMM dd})";

            // Get project members to notify
            var projectMembers = await dbContext.UserProjects
                .AsNoTracking()
                .Where(up => up.ProjectId == task.ProjectId)
                .Select(up => up.UserId)
                .Distinct()
                .ToListAsync(ct);
            var superAdminUserIds = await dbContext.UserRoles
                .AsNoTracking()
                .Join(
                    dbContext.Roles.AsNoTracking(),
                    userRole => userRole.RoleId,
                    role => role.Id,
                    (userRole, role) => new { userRole.UserId, role.Name })
                .Where(item => item.Name == AppRoles.SuperAdmin)
                .Join(
                    dbContext.Users.AsNoTracking(),
                    roleItem => roleItem.UserId,
                    user => user.Id,
                    (roleItem, user) => new { roleItem.UserId, user.IsActive, user.IsArchived })
                .Where(item => item.IsActive && !item.IsArchived)
                .Select(item => item.UserId)
                .Distinct()
                .ToListAsync(ct);

            var recipientUserIds = projectMembers
                .Concat(superAdminUserIds)
                .Distinct()
                .ToList();
            var superAdminsOutsideProject = superAdminUserIds
                .Except(projectMembers, StringComparer.Ordinal)
                .ToList();

            var notifications = recipientUserIds.Select(userId => new Notification
            {
                Id = Guid.NewGuid(),
                RecipientUserId = userId,
                ActorUserId = null,
                ActorName = "System",
                Type = notificationType,
                Message = message,
                TaskId = task.Id,
                ProjectId = task.ProjectId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
            }).ToList();

            if (notifications.Count > 0)
            {
                await dbContext.Notifications.AddRangeAsync(notifications, ct);
                await dbContext.SaveChangesAsync(ct);

                // Push via SignalR
                var pushDto = new NotificationDto
                {
                    Id = Guid.NewGuid(),
                    ActorUserId = null,
                    ActorName = "System",
                    Type = notificationType,
                    Message = message,
                    TaskId = task.Id,
                    ProjectId = task.ProjectId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                };

                await pushService.SendToProjectAsync(task.ProjectId, pushDto, ct);

                foreach (var userId in superAdminsOutsideProject)
                {
                    await pushService.SendToUserAsync(
                        userId,
                        new NotificationDto
                        {
                            Id = Guid.NewGuid(),
                            ActorUserId = null,
                            ActorName = "System",
                            Type = notificationType,
                            Message = message,
                            TaskId = task.Id,
                            ProjectId = task.ProjectId,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow,
                        },
                        ct);
                }
            }
        }
    }
}
