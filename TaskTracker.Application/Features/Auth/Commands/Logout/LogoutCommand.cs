using MediatR;

namespace TaskTracker.Application.Features.Auth.Commands.Logout;

public class LogoutCommand : IRequest<LogoutResponse>
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutResponse
{
    public string Message { get; set; } = string.Empty;
}
