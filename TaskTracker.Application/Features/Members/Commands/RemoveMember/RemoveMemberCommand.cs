using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Members.Commands.RemoveMember;

public sealed class RemoveMemberCommand : IRequest<bool>, IAuthorizedRequest
{
    public ScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public string RequiredPermission => AppPermissions.MembersRemove;
    public IReadOnlyList<ResourceScope> Scopes => ScopeType switch
    {
        ScopeType.Organization => [new ResourceScope(ResourceType.Organization, ScopeId)],
        ScopeType.Project => [new ResourceScope(ResourceType.Project, ScopeId)],
        _ => []
    };
}
