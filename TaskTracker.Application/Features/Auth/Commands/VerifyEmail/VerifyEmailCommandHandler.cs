using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;
namespace TaskTracker.Application.Features.Auth.Commands.VerifyEmail;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, VerifyEmailResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOtpRepository _otpRepository;
    private readonly IOtpService _otpService;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        UserManager<ApplicationUser> userManager,
        IOtpRepository otpRepository,
        IOtpService otpService,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _userManager = userManager;
        _otpRepository = otpRepository;
        _otpService = otpService;
        _logger = logger;
    }

    public async Task<VerifyEmailResponse> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.EmailConfirmed)
            return new VerifyEmailResponse { Success = true, Message = "Email is already verified." };

        // Get the latest active OTP for email verification
        var otpEntry = await _otpRepository.GetLatestActiveByUserIdAndPurposeAsync(
            user.Id, OtpPurpose.EmailVerification, cancellationToken)
            ?? throw new InvalidOperationException("No active OTP found. Please request a new one.");

        // Check expiry
        if (otpEntry.IsExpired)
            throw new InvalidOperationException("OTP has expired. Please request a new one.");

        // Check attempt limit
        if (otpEntry.AttemptCount >= 5)
            throw new InvalidOperationException("Maximum verification attempts exceeded. Please request a new OTP.");

        // Increment attempt count
        otpEntry.IncrementAttempt();

        // Verify OTP
        if (!_otpService.VerifyOtp(request.OtpCode, otpEntry.CodeHash))
        {
            await _otpRepository.UpdateAsync(otpEntry, cancellationToken);
            var remaining = 5 - otpEntry.AttemptCount;
            throw new InvalidOperationException($"Invalid OTP code. {remaining} attempt(s) remaining.");
        }

        // OTP is valid — mark as used and confirm email
        otpEntry.MarkUsed();
        user.EmailConfirmed = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);
        await _otpRepository.UpdateAsync(otpEntry, cancellationToken);

        _logger.LogInformation("Email verified for user: {Email}", request.Email);

        return new VerifyEmailResponse
        {
            Success = true,
            Message = "Email verified successfully. You can now log in."
        };
    }
}
