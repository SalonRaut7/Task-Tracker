using FluentValidation;

namespace TaskTracker.Application.Features.Users.Commands.UpdateCurrentUserProfile;

public sealed class UpdateCurrentUserProfileCommandValidator : AbstractValidator<UpdateCurrentUserProfileCommand>
{
    public UpdateCurrentUserProfileCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100)
            .Matches("^[A-Za-z][A-Za-z\\s'-]*$")
            .WithMessage("First name can contain letters, spaces, apostrophes, and hyphens only.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100)
            .Matches("^[A-Za-z][A-Za-z\\s'-]*$")
            .WithMessage("Last name can contain letters, spaces, apostrophes, and hyphens only.");
    }
}