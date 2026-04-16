using MediatR;

namespace TaskTracker.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommand : IRequest<ResetPasswordResponse>
{
    public string Email { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ResetPasswordResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
