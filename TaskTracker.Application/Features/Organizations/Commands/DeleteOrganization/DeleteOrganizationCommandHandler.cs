using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Organizations.Commands.DeleteOrganization;

public sealed class DeleteOrganizationCommandHandler : IRequestHandler<DeleteOrganizationCommand, bool>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationPushService _pushService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;

    public DeleteOrganizationCommandHandler(
        IOrganizationRepository organizationRepository,
        IMembershipRepository membershipRepository,
        INotificationRepository notificationRepository,
        INotificationPushService pushService,
        ICurrentUserService currentUser,
        IUserRepository userRepository)
    {
        _organizationRepository = organizationRepository;
        _membershipRepository = membershipRepository;
        _notificationRepository = notificationRepository;
        _pushService = pushService;
        _currentUser = currentUser;
        _userRepository = userRepository;
    }

    public async Task<bool> Handle(DeleteOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (organization is null)
        {
            return false;
        }

        var organizationMembers = await _membershipRepository.GetOrganizationMembershipsAsync(organization.Id, cancellationToken);
        var organizationMemberUserIds = organizationMembers.Select(m => m.UserId).Distinct().ToList();
        var superAdminUserIds = await _userRepository.GetSuperAdminUserIdsAsync(cancellationToken);
        var recipientUserIds = organizationMemberUserIds
            .Concat(superAdminUserIds)
            .Distinct()
            .ToList();
        var superAdminsOutsideOrganization = superAdminUserIds
            .Except(organizationMemberUserIds, StringComparer.Ordinal)
            .ToList();

        var actorUserId = _currentUser.UserId;
        var actorName = !string.IsNullOrWhiteSpace(actorUserId)
            ? await _userRepository.GetFullNameAsync(actorUserId, cancellationToken) ?? "Unknown"
            : "Unknown";
        var message = $"{actorName} deleted organization {organization.Name}";

        await _organizationRepository.DeleteAsync(organization, cancellationToken);

        if (recipientUserIds.Count > 0)
        {
            var notifications = recipientUserIds.Select(recipientId => new Notification
            {
                Id = Guid.NewGuid(),
                RecipientUserId = recipientId,
                ActorUserId = actorUserId,
                ActorName = actorName,
                Type = "OrganizationDeleted",
                Message = message,
                TaskId = null,
                ProjectId = null,
                IsRead = string.Equals(recipientId, actorUserId, StringComparison.Ordinal),
                CreatedAt = DateTime.UtcNow,
            }).ToList();

            await _notificationRepository.AddRangeAsync(notifications, cancellationToken);

            await _pushService.SendToOrganizationAsync(
                organization.Id,
                new NotificationDto
                {
                    Id = Guid.NewGuid(),
                    ActorUserId = actorUserId,
                    ActorName = actorName,
                    Type = "OrganizationDeleted",
                    Message = message,
                    TaskId = null,
                    ProjectId = null,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                },
                cancellationToken);

            foreach (var userId in superAdminsOutsideOrganization)
            {
                await _pushService.SendToUserAsync(
                    userId,
                    new NotificationDto
                    {
                        Id = Guid.NewGuid(),
                        ActorUserId = actorUserId,
                        ActorName = actorName,
                        Type = "OrganizationDeleted",
                        Message = message,
                        TaskId = null,
                        ProjectId = null,
                        IsRead = string.Equals(userId, actorUserId, StringComparison.Ordinal),
                        CreatedAt = DateTime.UtcNow,
                    },
                    cancellationToken);
            }
        }

        return true;
    }
}
