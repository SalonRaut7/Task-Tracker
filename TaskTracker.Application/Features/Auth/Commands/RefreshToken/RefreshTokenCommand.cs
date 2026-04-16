using MediatR;
using TaskTracker.Application.DTOs.Auth;

namespace TaskTracker.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommand : IRequest<AuthResponseDto>
{
    public string RefreshToken { get; set; } = string.Empty;
}
