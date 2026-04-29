using MediatR;
using TaskTracker.Application.DTOs;

namespace TaskTracker.Application.Features.Invitations.Commands.ResendInvitation;

public sealed class ResendInvitationCommand : IRequest<InvitationDto>
{
    public Guid InvitationId { get; set; }
}
