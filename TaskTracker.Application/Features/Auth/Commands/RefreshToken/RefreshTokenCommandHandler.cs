using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskTracker.Application.DTOs.Auth;
using TaskTracker.Application.Options;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Interfaces;
namespace TaskTracker.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IOptions<JwtOptions> jwtOptions,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Find the refresh token in DB and derive the user from persisted token ownership.
        var refreshTokenHash = _tokenService.HashToken(request.RefreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(refreshTokenHash, cancellationToken)
            ?? throw new InvalidOperationException("Invalid refresh token.");

        if (!storedToken.IsActive)
            throw new InvalidOperationException("Refresh token is expired or revoked.");

        var userId = storedToken.UserId;

        // Get the user
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        // Rotate tokens — revoke old, generate new
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _tokenService.HashToken(newRefreshToken);

        storedToken.Revoke(newRefreshTokenHash);

        // Keep JWT/global role claims lean: only SuperAdmin is emitted.
        var allRoles = await _userManager.GetRolesAsync(user);
        var globalRoles = allRoles
            .Where(r => string.Equals(r, AppRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var newAccessToken = _tokenService.GenerateAccessToken(user, globalRoles);


        // Store new refresh token
        await _refreshTokenRepository.AddAsync(new TaskTracker.Domain.Entities.Identity.RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays)
        }, cancellationToken);
        _logger.LogInformation("Token refreshed for user: {UserId}", userId);

        return new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = globalRoles
            }
        };
    }
}
