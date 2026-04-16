using MediatR;
using Microsoft.Extensions.Logging;
using TaskTracker.Domain.Interfaces;
namespace TaskTracker.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, LogoutResponse>
{

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        ILogger<LogoutCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _logger = logger;
    }
    public async Task<LogoutResponse> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var hash = _tokenService.HashToken(request.RefreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(hash, cancellationToken);

        if (storedToken is not null && storedToken.IsActive)
        {
            storedToken.Revoke();
            await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);
            _logger.LogInformation("Refresh token revoked for user: {UserId}", storedToken.UserId);
        }

        return new LogoutResponse { Message = "Logged out successfully." };
    }
}
