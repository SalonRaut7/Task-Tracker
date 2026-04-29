using FluentValidation;

namespace TaskTracker.Application.Features.Members.Commands.RemoveMember;

public sealed class RemoveMemberCommandValidator : AbstractValidator<RemoveMemberCommand>
{
    public RemoveMemberCommandValidator()
    {
        RuleFor(command => command.ScopeType)
            .IsInEnum().WithMessage("Invalid scope type.");

        RuleFor(command => command.ScopeId)
            .NotEmpty().WithMessage("Scope ID is required.");

        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}