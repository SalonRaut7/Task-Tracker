using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Interfaces;
/// Sends transactional emails (OTP, password reset, etc.).
public interface IEmailSender
{
    /// <summary>Sends an OTP code to the user's email.</summary>
    Task SendOtpAsync(string toEmail, string otpCode, OtpPurpose purpose, CancellationToken cancellationToken = default);

    /// <summary>Sends a password reset link/token to the user's email.</summary>
    Task SendPasswordResetAsync(string toEmail, string resetToken, CancellationToken cancellationToken = default);
}
