using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Features.Members.Queries.GetMembersByScope;

public sealed class GetMembersByScopeQuery : IRequest<ScopeMembersDto>
{
    public ScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
}
