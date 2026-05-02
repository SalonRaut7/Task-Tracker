using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Users.Commands.RestoreUser;

public sealed class RestoreUserCommandHandler : IRequestHandler<RestoreUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public RestoreUserCommandHandler(IUserRepository userRepository, ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
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
