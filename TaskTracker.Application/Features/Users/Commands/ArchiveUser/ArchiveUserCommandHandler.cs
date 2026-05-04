using MediatR;
using Microsoft.AspNetCore.Identity;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Users.Commands.ArchiveUser;

public sealed class ArchiveUserCommandHandler : IRequestHandler<ArchiveUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationPushService _notificationPush;

    public ArchiveUserCommandHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUser,
        UserManager<ApplicationUser> userManager,
        INotificationPushService notificationPush)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _userManager = userManager;
        _notificationPush = notificationPush;
    }

    public async Task<bool> Handle(ArchiveUserCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = EnsureSuperAdmin();

        if (string.Equals(request.UserId, currentUserId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("You cannot archive your own account.");
        }

        var user = await _userRepository.GetByIdForUpdateAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        if (user.IsArchived)
        {
            return true;
        }

        var isSuperAdminTarget = await _userManager.IsInRoleAsync(user, AppRoles.SuperAdmin);
        if (isSuperAdminTarget)
        {
            throw new InvalidOperationException("SuperAdmin account cannot be archived.");
        }

        await _userRepository.ArchiveAsync(user, currentUserId, request.Reason, cancellationToken);

        // Push force-logout to all active sessions of the archived user
        await _notificationPush.SendUserArchivedAsync(request.UserId, request.Reason, cancellationToken);

        return true;
    }

    private string EnsureSuperAdmin()
    {
        if (!_currentUser.IsSuperAdmin)
        {
            throw new ForbiddenAccessException("Only SuperAdmin can manage users.");
        }

        return _currentUser.UserId!;
    }
}
