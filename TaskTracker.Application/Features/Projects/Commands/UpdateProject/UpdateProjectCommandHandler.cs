using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Interfaces;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Projects.Commands.UpdateProject;

public sealed class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ProjectDto?>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly INotificationDispatchService _notificationDispatchService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;

    public UpdateProjectCommandHandler(
        IProjectRepository projectRepository,
        IMembershipRepository membershipRepository,
        INotificationDispatchService notificationDispatchService,
        ICurrentUserService currentUser,
        IUserRepository userRepository)
    {
        _projectRepository = projectRepository;
        _membershipRepository = membershipRepository;
        _notificationDispatchService = notificationDispatchService;
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

        var dto = ProjectDtoMapper.ToDto(project);

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

        var nowUtc = DateTime.UtcNow;
        var notifications = recipientUserIds.Select(recipientId => Notification.Create(
            recipientId,
            actorUserId,
            actorName,
            "ProjectUpdated",
            message,
            null,
            project.Id,
            string.Equals(recipientId, actorUserId, StringComparison.Ordinal),
            nowUtc)).ToList();

        if (notifications.Count > 0)
        {
            await _notificationDispatchService.DispatchAsync(notifications, cancellationToken);
        }

        return dto;
    }
}
