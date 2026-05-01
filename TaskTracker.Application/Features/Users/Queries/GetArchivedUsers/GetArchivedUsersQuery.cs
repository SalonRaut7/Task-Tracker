using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Users.Queries.GetArchivedUsers;

public sealed class GetArchivedUsersQuery : IRequest<PagedResultDto<UserSummaryDto>>, IAuthorizedRequest
{
    public string? Search { get; set; }
    public int? Skip { get; set; }
    public int? Take { get; set; }

    public string RequiredPermission => AppPermissions.UsersView;
    public IReadOnlyList<ResourceScope> Scopes => [];
}
