using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Features.Invitations.Commands.CreateInvitation;

public sealed class CreateInvitationCommand : IRequest<InvitationDto>, IAuthorizedRequest
{
    public ScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
    public string InviteeEmail { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public string RequiredPermission => AppPermissions.InvitationsCreate;
    public IReadOnlyList<ResourceScope> Scopes => ScopeType switch
    {
        ScopeType.Organization => [new ResourceScope(ResourceType.Organization, ScopeId)],
        ScopeType.Project => [new ResourceScope(ResourceType.Project, ScopeId)],
        _ => []
    };
}
