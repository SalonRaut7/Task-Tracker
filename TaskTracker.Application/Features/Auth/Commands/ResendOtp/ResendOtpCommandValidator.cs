using FluentValidation;

namespace TaskTracker.Application.Features.Auth.Commands.ResendOtp;

public class ResendOtpCommandValidator : AbstractValidator<ResendOtpCommand>
{
    public ResendOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email address.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");
    }
}
