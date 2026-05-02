using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Invitations.Queries.GetInvitationsByScope;

public sealed class GetInvitationsByScopeQueryHandler
    : IRequestHandler<GetInvitationsByScopeQuery, IReadOnlyList<InvitationDto>>
{
    private readonly IInvitationRepository _invitationRepository;

    public GetInvitationsByScopeQueryHandler(IInvitationRepository invitationRepository)
    {
        _invitationRepository = invitationRepository;
    }

    public async Task<IReadOnlyList<InvitationDto>> Handle(
        GetInvitationsByScopeQuery request, CancellationToken cancellationToken)
    {
        var invitations = await _invitationRepository.GetByScopeAsync(
            request.ScopeType, request.ScopeId, cancellationToken);

        return invitations.Select(invitation => InvitationDtoMapper.ToDto(invitation)).ToList();
    }
}
