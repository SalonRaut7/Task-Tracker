using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskTracker.Application.DTOs.Auth;
using TaskTracker.Application.Options;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly JwtOptions _jwtOptions;
    private readonly IdentityOptions _identityOptions;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IOptions<JwtOptions> jwtOptions,
        IOptions<IdentityOptions> identityOptions,
        ILogger<LoginCommandHandler> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtOptions = jwtOptions.Value;
        _identityOptions = identityOptions.Value;
        _logger = logger;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new InvalidOperationException("Invalid email or password.");

        // Check if account is locked out
        if (await _userManager.IsLockedOutAsync(user))
            throw new InvalidOperationException("Account is locked. Please try again later.");

        // Check email confirmation
        if (!user.EmailConfirmed)
            throw new InvalidOperationException("Please verify your email before logging in.");

        // Check if user is archived
        if (user.IsArchived)
            throw new InvalidOperationException("Your account has been archived. Contact admin for reactivation.");

        // Check if user is active
        if (!user.IsActive)
            throw new InvalidOperationException("Your account has been deactivated. Contact support.");

        // Validate password and keep lockout behavior.
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);
            var failedAttempts = await _userManager.GetAccessFailedCountAsync(user);
            var maxAttempts = _identityOptions.Lockout.MaxFailedAccessAttempts;

            if (await _userManager.IsLockedOutAsync(user))
            {
                var lockoutMinutes = (int)Math.Round(_identityOptions.Lockout.DefaultLockoutTimeSpan.TotalMinutes);
                throw new InvalidOperationException(
                    $"Account locked after {maxAttempts} failed login attempts. Try again in {lockoutMinutes} minutes.");
            }

            var remainingAttempts = Math.Max(maxAttempts - failedAttempts, 0);

            throw new InvalidOperationException(
                $"Password incorrect. {remainingAttempts} login attempt(s) remaining before lockout.");

        }

        await _userManager.ResetAccessFailedCountAsync(user);

        // Keep JWT/global role claims lean: only SuperAdmin is emitted.
        var allRoles = await _userManager.GetRolesAsync(user);
        var globalRoles = allRoles
            .Where(r => string.Equals(r, AppRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Generate lean tokens (no permissions in JWT — fetched via /api/me/permissions)
        var accessToken = _tokenService.GenerateAccessToken(user, globalRoles);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.HashToken(refreshToken);

        // Store refresh token
        var refreshTokenEntry = new TaskTracker.Domain.Entities.Identity.RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays)
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntry, cancellationToken);

        _logger.LogInformation("User logged in: {Email}", request.Email);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
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
