using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Interfaces;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Organizations.Commands.UpdateOrganization;

public sealed class UpdateOrganizationCommandHandler : IRequestHandler<UpdateOrganizationCommand, OrganizationDto?>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly INotificationDispatchService _notificationDispatchService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;

    public UpdateOrganizationCommandHandler(
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

    public async Task<OrganizationDto?> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (organization is null)
        {
            return null;
        }

        var oldName = organization.Name;
        var oldSlug = organization.Slug;
        var oldDescription = organization.Description;

        organization.Name = request.Name.Trim();
        organization.Slug = request.Slug.Trim().ToLowerInvariant();
        organization.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        organization.UpdatedAt = DateTime.UtcNow;

        await _organizationRepository.UpdateAsync(organization, cancellationToken);

        var dto = OrganizationDtoMapper.ToDto(organization);

        if (string.Equals(oldName, organization.Name, StringComparison.Ordinal)
            && string.Equals(oldSlug, organization.Slug, StringComparison.Ordinal)
            && string.Equals(oldDescription, organization.Description, StringComparison.Ordinal))
        {
            return dto;
        }

        var actorUserId = _currentUser.UserId;
        var actorName = !string.IsNullOrWhiteSpace(actorUserId)
            ? await _userRepository.GetFullNameAsync(actorUserId, cancellationToken) ?? "Unknown"
            : "Unknown";
        var message = $"{actorName} updated organization {organization.Name}";

        var organizationMembers = await _membershipRepository.GetOrganizationMembershipsAsync(organization.Id, cancellationToken);
        var organizationMemberUserIds = organizationMembers.Select(m => m.UserId).Distinct().ToList();
        var superAdminUserIds = await _userRepository.GetSuperAdminUserIdsAsync(cancellationToken);
        var recipientUserIds = organizationMemberUserIds
            .Concat(superAdminUserIds)
            .Distinct()
            .ToList();

        var nowUtc = DateTime.UtcNow;
        var notifications = recipientUserIds.Select(recipientId => Notification.Create(
            recipientId,
            actorUserId,
            actorName,
            "OrganizationUpdated",
            message,
            null,
            null,
            string.Equals(recipientId, actorUserId, StringComparison.Ordinal),
            nowUtc)).ToList();

        if (notifications.Count > 0)
        {
            await _notificationDispatchService.DispatchAsync(notifications, cancellationToken);
        }

        return dto;
    }
}
