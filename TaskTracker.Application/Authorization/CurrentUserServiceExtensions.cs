using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Authorization;

public static class CurrentUserServiceExtensions
{
    public static string RequireUserId(this ICurrentUserService currentUser)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        return currentUser.UserId;
    }
}
