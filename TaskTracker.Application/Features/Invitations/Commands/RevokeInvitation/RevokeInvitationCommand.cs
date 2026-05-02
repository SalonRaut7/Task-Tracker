using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Invitations.Commands.RevokeInvitation;

public sealed class RevokeInvitationCommand : IRequest<bool>, IAuthorizedRequest
{
    public Guid InvitationId { get; set; }

    public string RequiredPermission => AppPermissions.InvitationsRevoke;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Invitation, InvitationId)];
}
