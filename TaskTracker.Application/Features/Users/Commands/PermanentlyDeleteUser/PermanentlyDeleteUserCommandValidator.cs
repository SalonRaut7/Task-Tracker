using FluentValidation;

namespace TaskTracker.Application.Features.Users.Commands.PermanentlyDeleteUser;

public sealed class PermanentlyDeleteUserCommandValidator : AbstractValidator<PermanentlyDeleteUserCommand>
{
    public PermanentlyDeleteUserCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
