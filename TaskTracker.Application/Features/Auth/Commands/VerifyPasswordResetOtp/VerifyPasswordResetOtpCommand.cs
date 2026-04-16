using MediatR;

namespace TaskTracker.Application.Features.Auth.Commands.VerifyPasswordResetOtp;

public class VerifyPasswordResetOtpCommand : IRequest<VerifyPasswordResetOtpResponse>
{
    public string Email { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
}

public class VerifyPasswordResetOtpResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
