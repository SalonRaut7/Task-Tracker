using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Organizations.Commands.CreateOrganization;

public sealed class CreateOrganizationCommand : IRequest<OrganizationDto>, IAuthorizedRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string RequiredPermission => AppPermissions.OrganizationsCreate;
    public IReadOnlyList<ResourceScope> Scopes => [];
}
