using MediatR;
using TaskTracker.Application.DTOs;

namespace TaskTracker.Application.Features.Users.Queries.GetCurrentUserProfile;

public sealed class GetCurrentUserProfileQuery : IRequest<CurrentUserProfileDto> { }