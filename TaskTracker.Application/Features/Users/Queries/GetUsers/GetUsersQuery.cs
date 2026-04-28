using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Users.Queries.GetUsers;

public sealed class GetUsersQuery : IRequest<IReadOnlyList<UserSummaryDto>>, IAuthorizedRequest
{
    public string RequiredPermission => AppPermissions.UsersView;
    public IReadOnlyList<ResourceScope> Scopes => [];
}
