using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.Constants;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Users.Commands.RestoreUser;

public sealed class RestoreUserCommandHandler : IRequestHandler<RestoreUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cache;

    public RestoreUserCommandHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUser,
        ICacheService cache)
    {
        _userRepository = userRepository;
        _currentUser    = currentUser;
        _cache          = cache;
    }

    public async Task<bool> Handle(RestoreUserCommand request, CancellationToken cancellationToken)
    {
        EnsureSuperAdmin();

        var user = await _userRepository.GetByIdForUpdateAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        if (!user.IsArchived)
        {
            return true;
        }

        await _userRepository.RestoreAsync(user, cancellationToken);

        // Invalidate the cached status so the user can authenticate again immediately.
        _cache.Remove(CacheKeys.UserStatus(request.UserId));
        _cache.Remove(CacheKeys.UserPermissions(request.UserId));

        return true;
    }

    private void EnsureSuperAdmin()
    {
        if (!_currentUser.IsSuperAdmin)
        {
            throw new ForbiddenAccessException("Only SuperAdmin can manage users.");
        }
    }
}
