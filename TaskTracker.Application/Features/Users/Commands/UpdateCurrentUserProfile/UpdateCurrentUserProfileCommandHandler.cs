using MediatR;
using Microsoft.AspNetCore.Identity;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Users.Commands.UpdateCurrentUserProfile;

public sealed class UpdateCurrentUserProfileCommandHandler : IRequestHandler<UpdateCurrentUserProfileCommand, CurrentUserProfileDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;

    public UpdateCurrentUserProfileCommandHandler(
        ICurrentUserService currentUser,
        UserManager<ApplicationUser> userManager)
    {
        _currentUser = currentUser;
        _userManager = userManager;
    }

    public async Task<CurrentUserProfileDto> Handle(UpdateCurrentUserProfileCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.IsSuperAdmin)
        {
            throw new ForbiddenAccessException("SuperAdmin cannot update their own profile.");
        }

        var userId = _currentUser.RequireUserId();
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            throw new InvalidOperationException("Current user profile was not found.");
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Profile update failed: {errors}");
        }

        return new CurrentUserProfileDto
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
        };
    }
}
