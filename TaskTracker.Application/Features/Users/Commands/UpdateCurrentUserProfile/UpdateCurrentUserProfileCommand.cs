using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;

namespace TaskTracker.Application.Features.Users.Commands.UpdateCurrentUserProfile;

public sealed class UpdateCurrentUserProfileCommand : IRequest<CurrentUserProfileDto>, IAuthenticatedRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
