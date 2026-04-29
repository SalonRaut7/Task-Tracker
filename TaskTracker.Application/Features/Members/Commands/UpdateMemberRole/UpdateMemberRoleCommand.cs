using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Members.Commands.UpdateMemberRole;

public sealed class UpdateMemberRoleCommand : IRequest<MemberDto>, IAuthorizedRequest
{
    public ScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string NewRole { get; set; } = string.Empty;

    public string RequiredPermission => AppPermissions.MembersUpdateRole;
    public IReadOnlyList<ResourceScope> Scopes => ScopeType switch
    {
        ScopeType.Organization => [new ResourceScope(ResourceType.Organization, ScopeId)],
        ScopeType.Project => [new ResourceScope(ResourceType.Project, ScopeId)],
        _ => []
    };
}
