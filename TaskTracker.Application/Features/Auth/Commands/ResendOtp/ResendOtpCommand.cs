using MediatR;

namespace TaskTracker.Application.Features.Auth.Commands.ResendOtp;

public class ResendOtpCommand : IRequest<ResendOtpResponse>
{
    public string Email { get; set; } = string.Empty;
}

public class ResendOtpResponse
{
    public string Message { get; set; } = string.Empty;
}
