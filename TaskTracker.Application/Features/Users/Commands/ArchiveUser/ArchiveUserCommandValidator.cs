using FluentValidation;

namespace TaskTracker.Application.Features.Users.Commands.ArchiveUser;

public sealed class ArchiveUserCommandValidator : AbstractValidator<ArchiveUserCommand>
{
    public ArchiveUserCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(command => command.Reason)
            .MaximumLength(500)
            .WithMessage("Archive reason cannot exceed 500 characters.")
            .When(command => !string.IsNullOrWhiteSpace(command.Reason));
    }
}
