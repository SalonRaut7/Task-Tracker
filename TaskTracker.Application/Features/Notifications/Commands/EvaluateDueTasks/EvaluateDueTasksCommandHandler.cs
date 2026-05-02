using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskTracker.Application.Interfaces;
using TaskTracker.Application.Options;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Notifications.Commands.EvaluateDueTasks;

public sealed class EvaluateDueTasksCommandHandler : IRequestHandler<EvaluateDueTasksCommand, Unit>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationDispatchService _notificationDispatchService;
    private readonly NotificationOptions _notificationOptions;

    public EvaluateDueTasksCommandHandler(
        ITaskRepository taskRepository,
        IMembershipRepository membershipRepository,
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        INotificationDispatchService notificationDispatchService,
        IOptions<NotificationOptions> notificationOptions)
    {
        _taskRepository = taskRepository;
        _membershipRepository = membershipRepository;
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _notificationDispatchService = notificationDispatchService;
        _notificationOptions = notificationOptions.Value;
    }

    public async Task<Unit> Handle(EvaluateDueTasksCommand request, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(nowUtc);
        var dueSoonThreshold = DateOnly.FromDateTime(nowUtc.AddHours(_notificationOptions.DueSoonWindowHours));

        var dueTasks = await _taskRepository.Query()
            .Where(task => task.EndDate.HasValue
                && task.Status != Status.Completed
                && task.Status != Status.Cancelled
                && task.EndDate.Value <= dueSoonThreshold)
            .Select(task => new DueTaskProjection
            {
                Id = task.Id,
                Title = task.Title,
                ProjectId = task.ProjectId,
                EndDate = task.EndDate!.Value
            })
            .ToListAsync(cancellationToken);

        if (dueTasks.Count == 0)
        {
            return Unit.Value;
        }

        var superAdminUserIds = await _userRepository.GetSuperAdminUserIdsAsync(cancellationToken);

        foreach (var task in dueTasks)
        {
            var isOverdue = task.EndDate < today;
            var notificationType = isOverdue ? "TaskOverdue" : "TaskDueSoon";
            var sentSinceUtc = DateTime.UtcNow.Date;

            var alreadySent = await _notificationRepository.ExistsForTaskTypeSinceAsync(
                notificationType,
                task.Id,
                sentSinceUtc,
                cancellationToken);

            if (alreadySent)
            {
                continue;
            }

            var taskRef = string.IsNullOrWhiteSpace(task.Title)
                ? $"TASK-{task.Id}"
                : $"{task.Title} (TASK-{task.Id})";

            var message = isOverdue
                ? $"{taskRef} is past its due date ({task.EndDate:MMM dd})"
                : $"{taskRef} is due within {_notificationOptions.DueSoonWindowHours} hours ({task.EndDate:MMM dd})";

            var projectMembers = await _membershipRepository.GetProjectMembershipsAsync(task.ProjectId, cancellationToken);
            var projectMemberUserIds = projectMembers.Select(membership => membership.UserId).Distinct().ToList();
            var recipientUserIds = projectMemberUserIds
                .Concat(superAdminUserIds)
                .Distinct()
                .ToList();

            if (recipientUserIds.Count == 0)
            {
                continue;
            }

            var createdAtUtc = DateTime.UtcNow;
            var notifications = recipientUserIds.Select(recipientUserId => Notification.Create(
                recipientUserId,
                null,
                "System",
                notificationType,
                message,
                task.Id,
                task.ProjectId,
                false,
                createdAtUtc)).ToList();

            await _notificationDispatchService.DispatchAsync(notifications, cancellationToken);
        }

        return Unit.Value;
    }

    private sealed class DueTaskProjection
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public Guid ProjectId { get; init; }
        public DateOnly EndDate { get; init; }
    }
}
