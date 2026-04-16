using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskTracker.Application.Options;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;
namespace TaskTracker.Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOtpService _otpService;
    private readonly IEmailSender _emailSender;
    private readonly OtpOptions _otpOptions;
    private readonly IOtpRepository _otpRepository;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IOtpService otpService,
        IEmailSender emailSender,
        IOptions<OtpOptions> otpOptions,
        IOtpRepository otpRepository,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _otpService = otpService;
        _emailSender = emailSender;
        _otpOptions = otpOptions.Value;
        _otpRepository = otpRepository;
        _logger = logger;
    }

    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            throw new InvalidOperationException("Email does not exist.");
        }

        var otpCode = _otpService.GenerateOtp();

        // Find the latest active reset OTP entry and enforce resend protections.
        var otpEntry = await _otpRepository.GetLatestActiveByUserIdAndPurposeAsync(
            user.Id,
            OtpPurpose.PasswordReset,
            cancellationToken);

        if (otpEntry is not null)
        {
            if (!otpEntry.CanResend(_otpOptions.MaxResends, _otpOptions.ResendCooldownSeconds))
            {
                if (otpEntry.ResendCount >= _otpOptions.MaxResends)
                    throw new InvalidOperationException("Maximum resend attempts reached. Please try again later.");

                throw new InvalidOperationException(
                    $"Please wait {_otpOptions.ResendCooldownSeconds} seconds before requesting a new OTP.");
            }

            otpEntry.RecordResend(
                _otpService.HashOtp(otpCode),
                DateTime.UtcNow.AddMinutes(_otpOptions.ExpiryMinutes));

            await _otpRepository.UpdateAsync(otpEntry, cancellationToken);
        }
        else
        {
            var newEntry = new OtpEntry
            {
                UserId = user.Id,
                CodeHash = _otpService.HashOtp(otpCode),
                Purpose = OtpPurpose.PasswordReset,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_otpOptions.ExpiryMinutes),
                // Start cooldown immediately after first send.
                ResendCount = 1,
                LastResentAt = DateTime.UtcNow
            };

            await _otpRepository.AddAsync(newEntry, cancellationToken);
        }

        await _emailSender.SendPasswordResetAsync(request.Email, otpCode, cancellationToken);

        _logger.LogInformation("Password reset OTP sent to: {Email}", request.Email);

        return new ForgotPasswordResponse
        {
            Message = "Password reset code has been sent to your email."
        };
    }
}
