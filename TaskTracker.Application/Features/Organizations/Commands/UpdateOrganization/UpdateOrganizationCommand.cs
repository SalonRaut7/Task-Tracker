using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Organizations.Commands.UpdateOrganization;

public sealed class UpdateOrganizationCommand : IRequest<OrganizationDto?>, IAuthorizedRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string RequiredPermission => AppPermissions.OrganizationsUpdate;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Organization, Id)];
}
