using MediatR;
using Microsoft.AspNetCore.Identity;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Users.Commands.PermanentlyDeleteUser;

public sealed class PermanentlyDeleteUserCommandHandler : IRequestHandler<PermanentlyDeleteUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;

    public PermanentlyDeleteUserCommandHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUser,
        UserManager<ApplicationUser> userManager)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _userManager = userManager;
    }

    public async Task<bool> Handle(PermanentlyDeleteUserCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = EnsureSuperAdmin();

        if (string.Equals(request.UserId, currentUserId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("You cannot permanently delete your own account.");
        }

        var user = await _userRepository.GetByIdForUpdateAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        if (!user.IsArchived)
        {
            throw new InvalidOperationException("User must be archived before permanent deletion.");
        }

        await _userRepository.ReassignReportedTasksAsync(user.Id, currentUserId, cancellationToken);
        await _userRepository.ClearTaskAssignmentsAsync(user.Id, cancellationToken);

        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            var errors = string.Join("; ", deleteResult.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to permanently delete user: {errors}");
        }

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
