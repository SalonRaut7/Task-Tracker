using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Features.Invitations.Queries.GetInvitationsByScope;

public sealed class GetInvitationsByScopeQuery : IRequest<IReadOnlyList<InvitationDto>>
{
    public ScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
}
