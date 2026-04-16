using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskTracker.Application.Options;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;
namespace TaskTracker.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOtpService _otpService;
    private readonly IEmailSender _emailSender;
    private readonly OtpOptions _otpOptions;
    private readonly IOtpRepository _otpRepository;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        UserManager<ApplicationUser> userManager,
        IOtpService otpService,
        IEmailSender emailSender,
        IOptions<OtpOptions> otpOptions,
        IOtpRepository otpRepository,
        ILogger<RegisterCommandHandler> logger)
    {
        _userManager = userManager;
        _otpService = otpService;
        _emailSender = emailSender;
        _otpOptions = otpOptions.Value;
        _otpRepository = otpRepository;
        _logger = logger;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            throw new InvalidOperationException("A user with this email already exists.");

        // Create user
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"User registration failed: {errors}");
        }

        // Assign default role
        await _userManager.AddToRoleAsync(user, AppRoles.Viewer);

        // Generate and store OTP
        var otpCode = _otpService.GenerateOtp();
        var otpEntry = new OtpEntry
        {
            UserId = user.Id,
            CodeHash = _otpService.HashOtp(otpCode),
            Purpose = OtpPurpose.EmailVerification,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_otpOptions.ExpiryMinutes)
        };

        await _otpRepository.AddAsync(otpEntry, cancellationToken);

        // Send OTP email
        await _emailSender.SendOtpAsync(request.Email, otpCode, OtpPurpose.EmailVerification, cancellationToken);

        _logger.LogInformation("User registered: {Email}. OTP sent for verification.", request.Email);

        return new RegisterResponse
        {
            UserId = user.Id,
            Message = "Registration successful. Please check your email for the verification code."
        };
    }
}
