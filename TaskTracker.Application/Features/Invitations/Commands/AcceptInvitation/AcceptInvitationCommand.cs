using MediatR;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Features.Invitations.Commands.AcceptInvitation;

public sealed class AcceptInvitationCommand : IRequest<AcceptInvitationResult>
{
    public string Token { get; set; } = string.Empty;
}

public sealed class AcceptInvitationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public ScopeType? ScopeType { get; init; }
    public Guid? ScopeId { get; init; }
    public string? Role { get; init; }
}
