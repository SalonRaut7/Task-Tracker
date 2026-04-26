using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Organizations.Commands.CreateOrganization;

public sealed class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, OrganizationDto>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ICurrentUserService _currentUser;

    public CreateOrganizationCommandHandler(
        IOrganizationRepository organizationRepository,
        ICurrentUserService currentUser)
    {
        _organizationRepository = organizationRepository;
        _currentUser = currentUser;
    }

    public async Task<OrganizationDto> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Slug = request.Slug.Trim().ToLowerInvariant(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        // Auto-assign creator as OrgAdmin — ensures immediate ownership
        if (!string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            organization.UserMemberships.Add(new UserOrganization
            {
                UserId = _currentUser.UserId,
                OrganizationId = organization.Id,
                Role = AppRoles.OrgAdmin,
                JoinedAt = now,
                UpdatedAt = now
            });
        }

        await _organizationRepository.AddAsync(organization, cancellationToken);

        return new OrganizationDto
        {
            Id = organization.Id,
            Name = organization.Name,
            Slug = organization.Slug,
            Description = organization.Description,
            CreatedAt = organization.CreatedAt,
            UpdatedAt = organization.UpdatedAt
        };
    }
}
