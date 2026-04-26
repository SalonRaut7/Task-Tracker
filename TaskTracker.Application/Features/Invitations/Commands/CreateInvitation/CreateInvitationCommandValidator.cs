using FluentValidation;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Invitations.Commands.CreateInvitation;

public sealed class CreateInvitationCommandValidator : AbstractValidator<CreateInvitationCommand>
{
    public CreateInvitationCommandValidator()
    {
        RuleFor(x => x.ScopeType)
            .IsInEnum().WithMessage("Invalid scope type.");

        RuleFor(x => x.ScopeId)
            .NotEmpty().WithMessage("Scope ID is required.");

        RuleFor(x => x.InviteeEmail)
            .NotEmpty().WithMessage("Invitee email is required.")
            .EmailAddress().WithMessage("Invalid email address.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .MaximumLength(50).WithMessage("Role cannot exceed 50 characters.");

        RuleFor(x => x)
            .Must(x => AppRoles.IsValidForScope(x.Role, x.ScopeType))
            .WithMessage("Role is not valid for the selected scope type.");
    }
}
