using FluentValidation;

namespace TaskTracker.Application.Features.Invitations.Commands.RevokeInvitation;

public sealed class RevokeInvitationCommandValidator : AbstractValidator<RevokeInvitationCommand>
{
    public RevokeInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationId)
            .NotEmpty().WithMessage("Invitation ID is required.");
    }
}
