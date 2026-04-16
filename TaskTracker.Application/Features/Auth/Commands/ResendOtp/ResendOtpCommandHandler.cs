using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskTracker.Application.Options;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;
namespace TaskTracker.Application.Features.Auth.Commands.ResendOtp;

public class ResendOtpCommandHandler : IRequestHandler<ResendOtpCommand, ResendOtpResponse>
{

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOtpRepository _otpRepository;
    private readonly IOtpService _otpService;
    private readonly IEmailSender _emailSender;
    private readonly OtpOptions _otpOptions;
    private readonly ILogger<ResendOtpCommandHandler> _logger;
    public ResendOtpCommandHandler(
        UserManager<ApplicationUser> userManager,
        IOtpRepository otpRepository,
        IOtpService otpService,
        IEmailSender emailSender,
        IOptions<OtpOptions> otpOptions,
        ILogger<ResendOtpCommandHandler> logger)
    {
        _userManager = userManager;
        _otpRepository = otpRepository;
        _otpService = otpService;
        _emailSender = emailSender;
        _otpOptions = otpOptions.Value;
        _logger = logger;
    }

    public async Task<ResendOtpResponse> Handle(ResendOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.EmailConfirmed)
            throw new InvalidOperationException("Email is already verified.");

        // Find the latest OTP entry
        var otpEntry = await _otpRepository.GetLatestActiveByUserIdAndPurposeAsync(
            user.Id, OtpPurpose.EmailVerification, cancellationToken);

        if (otpEntry is not null)
        {
            // Check cooldown and resend limits
            if (!otpEntry.CanResend(_otpOptions.MaxResends, _otpOptions.ResendCooldownSeconds))
            {
                if (otpEntry.ResendCount >= _otpOptions.MaxResends)
                    throw new InvalidOperationException("Maximum resend attempts reached. Please register again or contact support.");

                throw new InvalidOperationException(
                    $"Please wait {_otpOptions.ResendCooldownSeconds} seconds before requesting a new OTP.");
            }

            // Update existing entry with new OTP
            var newOtp = _otpService.GenerateOtp();
            otpEntry.RecordResend(
                _otpService.HashOtp(newOtp),
                DateTime.UtcNow.AddMinutes(_otpOptions.ExpiryMinutes));

            await _otpRepository.UpdateAsync(otpEntry, cancellationToken);
            await _emailSender.SendOtpAsync(request.Email, newOtp, OtpPurpose.EmailVerification, cancellationToken);
        }
        else
        {
            // Create a new OTP entry
            var newOtp = _otpService.GenerateOtp();
            var newEntry = new OtpEntry
            {
                UserId = user.Id,
                CodeHash = _otpService.HashOtp(newOtp),
                Purpose = OtpPurpose.EmailVerification,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_otpOptions.ExpiryMinutes)
            };

            await _otpRepository.AddAsync(newEntry, cancellationToken);
            await _emailSender.SendOtpAsync(request.Email, newOtp, OtpPurpose.EmailVerification, cancellationToken);
        }

        _logger.LogInformation("OTP resent for user: {Email}", request.Email);

        return new ResendOtpResponse
        {
            Message = "A new verification code has been sent to your email."
        };
    }
}
