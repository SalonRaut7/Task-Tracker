using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Features.Members.Queries.GetMembersByScope;

public sealed class GetMembersByScopeQuery : IRequest<ScopeMembersDto>, IAuthorizedRequest
{
    public ScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }

    public string RequiredPermission => AppPermissions.MembersView;
    public IReadOnlyList<ResourceScope> Scopes => ScopeType switch
    {
        ScopeType.Organization => [new ResourceScope(ResourceType.Organization, ScopeId)],
        ScopeType.Project => [new ResourceScope(ResourceType.Project, ScopeId)],
        _ => []
    };
}
