using FluentValidation;

namespace TaskTracker.Application.Features.Auth.Commands.VerifyEmail;

public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress();

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("OTP code is required.")
            .Length(6).WithMessage("OTP code must be 6 digits.")
            .Matches("^[0-9]+$").WithMessage("OTP code must contain only digits.");
    }
}
