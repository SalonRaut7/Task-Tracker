using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Projects.Commands.UpdateProject;

public sealed class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ProjectDto?>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationPushService _pushService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;

    public UpdateProjectCommandHandler(
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

    public async Task<ProjectDto?> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var oldName = project.Name;
        var oldKey = project.Key;
        var oldDescription = project.Description;

        project.Name = request.Name.Trim();
        project.Key = request.Key.Trim().ToUpperInvariant();
        project.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        project.UpdatedAt = DateTime.UtcNow;

        await _projectRepository.UpdateAsync(project, cancellationToken);

        var dto = new ProjectDto
        {
            Id = project.Id,
            OrganizationId = project.OrganizationId,
            Name = project.Name,
            Key = project.Key,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };

        if (string.Equals(oldName, project.Name, StringComparison.Ordinal)
            && string.Equals(oldKey, project.Key, StringComparison.Ordinal)
            && string.Equals(oldDescription, project.Description, StringComparison.Ordinal))
        {
            return dto;
        }

        var actorUserId = _currentUser.UserId;
        var actorName = !string.IsNullOrWhiteSpace(actorUserId)
            ? await _userRepository.GetFullNameAsync(actorUserId, cancellationToken) ?? "Unknown"
            : "Unknown";
        var message = $"{actorName} updated project {project.Name}";

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

        var notifications = recipientUserIds.Select(recipientId => new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipientId,
            ActorUserId = actorUserId,
            ActorName = actorName,
            Type = "ProjectUpdated",
            Message = message,
            TaskId = null,
            ProjectId = project.Id,
            IsRead = string.Equals(recipientId, actorUserId, StringComparison.Ordinal),
            CreatedAt = DateTime.UtcNow,
        }).ToList();

        if (notifications.Count > 0)
        {
            await _notificationRepository.AddRangeAsync(notifications, cancellationToken);

            await _pushService.SendToProjectAsync(
                project.Id,
                new NotificationDto
                {
                    Id = Guid.NewGuid(),
                    ActorUserId = actorUserId,
                    ActorName = actorName,
                    Type = "ProjectUpdated",
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
                        Type = "ProjectUpdated",
                        Message = message,
                        TaskId = null,
                        ProjectId = project.Id,
                        IsRead = string.Equals(userId, actorUserId, StringComparison.Ordinal),
                        CreatedAt = DateTime.UtcNow,
                    },
                    cancellationToken);
            }
        }

        return dto;
    }
}
