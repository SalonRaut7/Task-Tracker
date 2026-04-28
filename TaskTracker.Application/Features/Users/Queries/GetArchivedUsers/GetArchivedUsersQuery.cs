using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Users.Queries.GetArchivedUsers;

public sealed class GetArchivedUsersQuery : IRequest<IReadOnlyList<UserSummaryDto>>, IAuthorizedRequest
{
    public string RequiredPermission => AppPermissions.UsersView;
    public IReadOnlyList<ResourceScope> Scopes => [];
}
