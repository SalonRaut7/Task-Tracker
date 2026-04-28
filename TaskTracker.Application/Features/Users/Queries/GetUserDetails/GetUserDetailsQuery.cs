using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Users.Queries.GetUserDetails;

public sealed class GetUserDetailsQuery : IRequest<UserDetailsDto?>, IAuthorizedRequest
{
    public string UserId { get; set; } = string.Empty;

    public string RequiredPermission => AppPermissions.UsersView;
    public IReadOnlyList<ResourceScope> Scopes => [];
}
