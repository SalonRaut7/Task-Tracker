using FluentValidation;

namespace TaskTracker.Application.Features.Invitations.Commands.ResendInvitation;

public sealed class ResendInvitationCommandValidator : AbstractValidator<ResendInvitationCommand>
{
    public ResendInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationId)
            .NotEmpty().WithMessage("Invitation ID is required.");
    }
}
