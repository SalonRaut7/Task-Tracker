using MediatR;
using TaskTracker.Application.DTOs;
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

        return invitations.Select(i => new InvitationDto
        {
            Id = i.Id,
            ScopeType = i.ScopeType,
            ScopeId = i.ScopeId,
            InviteeEmail = i.InviteeEmail,
            Role = i.Role,
            Status = i.IsExpired && i.Status == InvitationStatus.Pending
                ? InvitationStatus.Expired
                : i.Status,
            InvitedByUserId = i.InvitedByUserId,
            InvitedByName = i.InvitedByUser is not null
                ? $"{i.InvitedByUser.FirstName} {i.InvitedByUser.LastName}".Trim()
                : "",
            CreatedAt = i.CreatedAt,
            ExpiresAt = i.ExpiresAt,
            AcceptedAt = i.AcceptedAt,
            RevokedAt = i.RevokedAt
        }).ToList();
    }
}
