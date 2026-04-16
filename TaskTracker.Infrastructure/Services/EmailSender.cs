using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using TaskTracker.Application.Options;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Infrastructure.Services;

public class EmailSender : IEmailSender
{
    private readonly SmtpOptions _smtp;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<SmtpOptions> smtp, ILogger<EmailSender> logger)
    {
        _smtp = smtp.Value;
        _logger = logger;
    }

    public async Task SendOtpAsync(string toEmail, string otpCode, OtpPurpose purpose, CancellationToken cancellationToken = default)
    {
        var subject = purpose switch
        {
            OtpPurpose.EmailVerification => "TaskTracker — Verify Your Email",
            OtpPurpose.PasswordReset => "TaskTracker — Password Reset Code",
            _ => "TaskTracker — Your OTP Code"
        };

        var body = $@"
<html>
<body style='font-family: Arial, sans-serif; color: #333;'>
    <h2 style='color: #4F46E5;'>TaskTracker</h2>
    <p>Your verification code is:</p>
    <h1 style='letter-spacing: 8px; color: #4F46E5; font-size: 36px;'>{otpCode}</h1>
    <p>This code expires in <strong>5 minutes</strong>.</p>
    <p style='color: #888; font-size: 12px;'>If you did not request this code, please ignore this email.</p>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetToken, CancellationToken cancellationToken = default)
    {
        var subject = "TaskTracker — Reset Your Password";

        var body = $@"
<html>
<body style='font-family: Arial, sans-serif; color: #333;'>
    <h2 style='color: #4F46E5;'>TaskTracker</h2>
    <p>You requested a password reset. Use the code below:</p>
    <h1 style='letter-spacing: 8px; color: #4F46E5; font-size: 36px;'>{resetToken}</h1>
    <p>This code expires in <strong>15 minutes</strong>.</p>
    <p style='color: #888; font-size: 12px;'>If you did not request this, please ignore this email.</p>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        // If SMTP is not configured, fall back to logging
        if (string.IsNullOrWhiteSpace(_smtp.Host))
        {
            _logger.LogWarning("SMTP not configured. Email to {Email} | Subject: {Subject}", toEmail, subject);
            _logger.LogInformation("Email body (dev mode): {Body}", htmlBody);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.SenderName, _smtp.SenderEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_smtp.Host, _smtp.Port, _smtp.UseSsl, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_smtp.Username))
                await client.AuthenticateAsync(_smtp.Username, _smtp.Password, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            _logger.LogInformation("Email sent to {Email} — Subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
