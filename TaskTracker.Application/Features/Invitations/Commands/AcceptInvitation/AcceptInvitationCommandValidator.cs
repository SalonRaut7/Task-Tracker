using FluentValidation;

namespace TaskTracker.Application.Features.Invitations.Commands.AcceptInvitation;

public sealed class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Invitation token is required.")
            .MaximumLength(512).WithMessage("Invitation token is too long.");
    }
}
