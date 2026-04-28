using FluentValidation;

namespace TaskTracker.Application.Features.Users.Commands.RestoreUser;

public sealed class RestoreUserCommandValidator : AbstractValidator<RestoreUserCommand>
{
    public RestoreUserCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
