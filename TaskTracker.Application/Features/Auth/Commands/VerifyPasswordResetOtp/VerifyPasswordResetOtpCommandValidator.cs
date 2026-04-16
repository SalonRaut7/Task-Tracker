using FluentValidation;

namespace TaskTracker.Application.Features.Auth.Commands.VerifyPasswordResetOtp;

public class VerifyPasswordResetOtpCommandValidator : AbstractValidator<VerifyPasswordResetOtpCommand>
{
    public VerifyPasswordResetOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email.");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("Reset code is required.")
            .Length(6).WithMessage("Reset code must be 6 digits.")
            .Matches("^[0-9]+$").WithMessage("Reset code must contain only digits.");
    }
}
