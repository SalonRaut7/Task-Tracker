using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Projects.Commands.DeleteProject;

public sealed class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, bool>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationPushService _pushService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;

    public DeleteProjectCommandHandler(
        IProjectRepository projectRepository,
        IMembershipRepository membershipRepository,
        INotificationRepository notificationRepository,
        INotificationPushService pushService,
        ICurrentUserService currentUser,
        IUserRepository userRepository)
    {
        _projectRepository = projectRepository;
        _membershipRepository = membershipRepository;
        _notificationRepository = notificationRepository;
        _pushService = pushService;
        _currentUser = currentUser;
        _userRepository = userRepository;
    }

    public async Task<bool> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (project is null)
        {
            return false;
        }

        var projectMembers = await _membershipRepository.GetProjectMembershipsAsync(project.Id, cancellationToken);
        var projectMemberUserIds = projectMembers.Select(m => m.UserId).Distinct().ToList();
        var superAdminUserIds = await _userRepository.GetSuperAdminUserIdsAsync(cancellationToken);
        var recipientUserIds = projectMemberUserIds
            .Concat(superAdminUserIds)
            .Distinct()
            .ToList();
        var superAdminsOutsideProject = superAdminUserIds
            .Except(projectMemberUserIds, StringComparer.Ordinal)
            .ToList();

        var actorUserId = _currentUser.UserId;
        var actorName = !string.IsNullOrWhiteSpace(actorUserId)
            ? await _userRepository.GetFullNameAsync(actorUserId, cancellationToken) ?? "Unknown"
            : "Unknown";
        var message = $"{actorName} deleted project {project.Name}";

        await _projectRepository.DeleteAsync(project, cancellationToken);

        if (recipientUserIds.Count > 0)
        {
            var notifications = recipientUserIds.Select(recipientId => new Notification
            {
                Id = Guid.NewGuid(),
                RecipientUserId = recipientId,
                ActorUserId = actorUserId,
                ActorName = actorName,
                Type = "ProjectDeleted",
                Message = message,
                TaskId = null,
                ProjectId = project.Id,
                IsRead = string.Equals(recipientId, actorUserId, StringComparison.Ordinal),
                CreatedAt = DateTime.UtcNow,
            }).ToList();

            await _notificationRepository.AddRangeAsync(notifications, cancellationToken);

            await _pushService.SendToProjectAsync(
                project.Id,
                new NotificationDto
                {
                    Id = Guid.NewGuid(),
                    ActorUserId = actorUserId,
                    ActorName = actorName,
                    Type = "ProjectDeleted",
                    Message = message,
                    TaskId = null,
                    ProjectId = project.Id,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                },
                cancellationToken);

            foreach (var userId in superAdminsOutsideProject)
            {
                await _pushService.SendToUserAsync(
                    userId,
                    new NotificationDto
                    {
                        Id = Guid.NewGuid(),
                        ActorUserId = actorUserId,
                        ActorName = actorName,
                        Type = "ProjectDeleted",
                        Message = message,
                        TaskId = null,
                        ProjectId = project.Id,
                        IsRead = string.Equals(userId, actorUserId, StringComparison.Ordinal),
                        CreatedAt = DateTime.UtcNow,
                    },
                    cancellationToken);
            }
        }

        return true;
    }
}
