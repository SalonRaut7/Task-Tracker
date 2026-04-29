using FluentValidation;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Members.Commands.UpdateMemberRole;

public sealed class UpdateMemberRoleCommandValidator : AbstractValidator<UpdateMemberRoleCommand>
{
    public UpdateMemberRoleCommandValidator()
    {
        RuleFor(command => command.ScopeType)
            .IsInEnum().WithMessage("Invalid scope type.");

        RuleFor(command => command.ScopeId)
            .NotEmpty().WithMessage("Scope ID is required.");

        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(command => command.NewRole)
            .NotEmpty().WithMessage("New role is required.")
            .MaximumLength(50).WithMessage("New role cannot exceed 50 characters.");

        RuleFor(command => command)
            .Must(command => AppRoles.IsValidForScope(command.NewRole, command.ScopeType))
            .WithMessage("Role is not valid for the selected scope type.");
    }
}