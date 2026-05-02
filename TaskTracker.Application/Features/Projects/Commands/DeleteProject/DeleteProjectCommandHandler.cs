using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Projects.Commands.DeleteProject;

public sealed class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, bool>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly INotificationDispatchService _notificationDispatchService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;

    public DeleteProjectCommandHandler(
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

        var actorUserId = _currentUser.UserId;
        var actorName = !string.IsNullOrWhiteSpace(actorUserId)
            ? await _userRepository.GetFullNameAsync(actorUserId, cancellationToken) ?? "Unknown"
            : "Unknown";
        var message = $"{actorName} deleted project {project.Name}";

        await _projectRepository.DeleteAsync(project, cancellationToken);

        if (recipientUserIds.Count > 0)
        {
            var nowUtc = DateTime.UtcNow;
            var notifications = recipientUserIds.Select(recipientId => Notification.Create(
                recipientId,
                actorUserId,
                actorName,
                "ProjectDeleted",
                message,
                null,
                project.Id,
                string.Equals(recipientId, actorUserId, StringComparison.Ordinal),
                nowUtc)).ToList();

            await _notificationDispatchService.DispatchAsync(notifications, cancellationToken);
        }

        return true;
    }
}
