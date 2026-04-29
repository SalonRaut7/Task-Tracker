using MediatR;

namespace TaskTracker.Application.Features.Invitations.Commands.RevokeInvitation;

public sealed class RevokeInvitationCommand : IRequest<bool>
{
    public Guid InvitationId { get; set; }
}
