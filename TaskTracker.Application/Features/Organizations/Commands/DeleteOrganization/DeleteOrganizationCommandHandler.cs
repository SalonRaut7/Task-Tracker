using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Organizations.Commands.DeleteOrganization;

public sealed class DeleteOrganizationCommandHandler : IRequestHandler<DeleteOrganizationCommand, bool>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly INotificationDispatchService _notificationDispatchService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;

    public DeleteOrganizationCommandHandler(
        IOrganizationRepository organizationRepository,
        IMembershipRepository membershipRepository,
        INotificationDispatchService notificationDispatchService,
        ICurrentUserService currentUser,
        IUserRepository userRepository)
    {
        _organizationRepository = organizationRepository;
        _membershipRepository = membershipRepository;
        _notificationDispatchService = notificationDispatchService;
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

        var actorUserId = _currentUser.UserId;
        var actorName = !string.IsNullOrWhiteSpace(actorUserId)
            ? await _userRepository.GetFullNameAsync(actorUserId, cancellationToken) ?? "Unknown"
            : "Unknown";
        var message = $"{actorName} deleted organization {organization.Name}";

        await _organizationRepository.DeleteAsync(organization, cancellationToken);

        if (recipientUserIds.Count > 0)
        {
            var nowUtc = DateTime.UtcNow;
            var notifications = recipientUserIds.Select(recipientId => Notification.Create(
                recipientId,
                actorUserId,
                actorName,
                "OrganizationDeleted",
                message,
                null,
                null,
                string.Equals(recipientId, actorUserId, StringComparison.Ordinal),
                nowUtc)).ToList();

            await _notificationDispatchService.DispatchAsync(notifications, cancellationToken);
        }

        return true;
    }
}
