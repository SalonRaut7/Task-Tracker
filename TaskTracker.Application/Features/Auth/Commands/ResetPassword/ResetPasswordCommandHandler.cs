using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;
namespace TaskTracker.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOtpRepository _otpRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IOtpService _otpService;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IOtpRepository otpRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IOtpService otpService,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _otpRepository = otpRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _otpService = otpService;
        _logger = logger;
    }

    public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new KeyNotFoundException("User not found.");

        // Get the latest password-reset OTP
        var otpEntry = await _otpRepository.GetLatestActiveByUserIdAndPurposeAsync(
            user.Id, OtpPurpose.PasswordReset, cancellationToken)
            ?? throw new InvalidOperationException("No active password reset code found. Please request a new one.");

        if (otpEntry.IsExpired)
            throw new InvalidOperationException("Reset code has expired. Please request a new one.");

        if (otpEntry.AttemptCount >= 5)
            throw new InvalidOperationException("Maximum attempts exceeded. Please request a new reset code.");

        otpEntry.IncrementAttempt();

        if (!_otpService.VerifyOtp(request.OtpCode, otpEntry.CodeHash))
        {
            await _otpRepository.UpdateAsync(otpEntry, cancellationToken);
            var remaining = 5 - otpEntry.AttemptCount;
            throw new InvalidOperationException($"Invalid reset code. {remaining} attempt(s) remaining.");
        }

        // OTP valid — reset password
        otpEntry.MarkUsed();

        await _otpRepository.UpdateAsync(otpEntry, cancellationToken);

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Password reset failed: {errors}");
        }

        // Revoke all active refresh tokens (force re-login)
        var activeTokens = await _refreshTokenRepository.GetActiveTokensByUserIdAsync(user.Id, cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke();
            await _refreshTokenRepository.UpdateAsync(token, cancellationToken);
        }

        _logger.LogInformation("Password reset successfully for user: {Email}", request.Email);

        return new ResetPasswordResponse
        {
            Success = true,
            Message = "Password has been reset successfully. Please log in with your new password."
        };
    }
}
