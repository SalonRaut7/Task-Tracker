using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Auth.Commands.VerifyPasswordResetOtp;

public class VerifyPasswordResetOtpCommandHandler : IRequestHandler<VerifyPasswordResetOtpCommand, VerifyPasswordResetOtpResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOtpRepository _otpRepository;
    private readonly IOtpService _otpService;
    private readonly ILogger<VerifyPasswordResetOtpCommandHandler> _logger;

    public VerifyPasswordResetOtpCommandHandler(
        UserManager<ApplicationUser> userManager,
        IOtpRepository otpRepository,
        IOtpService otpService,
        ILogger<VerifyPasswordResetOtpCommandHandler> logger)
    {
        _userManager = userManager;
        _otpRepository = otpRepository;
        _otpService = otpService;
        _logger = logger;
    }

    public async Task<VerifyPasswordResetOtpResponse> Handle(VerifyPasswordResetOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new InvalidOperationException("Email does not exist.");

        var otpEntry = await _otpRepository.GetLatestActiveByUserIdAndPurposeAsync(
            user.Id,
            OtpPurpose.PasswordReset,
            cancellationToken)
            ?? throw new InvalidOperationException("No active password reset code found. Please request a new one.");

        if (otpEntry.IsExpired)
            throw new InvalidOperationException("Reset code has expired. Please request a new one.");

        if (otpEntry.AttemptCount >= 5)
            throw new InvalidOperationException("Maximum attempts exceeded. Please request a new reset code.");

        if (!_otpService.VerifyOtp(request.OtpCode, otpEntry.CodeHash))
        {
            otpEntry.IncrementAttempt();
            await _otpRepository.UpdateAsync(otpEntry, cancellationToken);

            var remaining = Math.Max(5 - otpEntry.AttemptCount, 0);
            throw new InvalidOperationException($"Invalid reset code. {remaining} attempt(s) remaining.");
        }

        _logger.LogInformation("Password reset OTP verified for user: {Email}", request.Email);

        return new VerifyPasswordResetOtpResponse
        {
            Success = true,
            Message = "Reset code verified. You can now set a new password."
        };
    }
}
