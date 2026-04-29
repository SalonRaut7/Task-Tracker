using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Users.Queries.GetCurrentUserProfile;

public sealed class GetCurrentUserProfileQueryHandler : IRequestHandler<GetCurrentUserProfileQuery, CurrentUserProfileDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;

    public GetCurrentUserProfileQueryHandler(
        ICurrentUserService currentUser,
        IUserRepository userRepository)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
    }

    public async Task<CurrentUserProfileDto> Handle(GetCurrentUserProfileQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        var profile = await _userRepository.GetUserDetailsAsync(_currentUser.UserId, cancellationToken);
        if (profile is null)
        {
            throw new InvalidOperationException("Current user profile was not found.");
        }

        return new CurrentUserProfileDto
        {
            UserId = profile.UserId,
            Email = profile.Email,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            FullName = $"{profile.FirstName} {profile.LastName}".Trim(),
        };
    }
}