using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Invitations.Commands.ResendInvitation;

public sealed class ResendInvitationCommand : IRequest<InvitationDto>, IAuthorizedRequest
{
    public Guid InvitationId { get; set; }

    public string RequiredPermission => AppPermissions.InvitationsCreate;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Invitation, InvitationId)];
}
