using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Users.Commands.ArchiveUser;

public sealed class ArchiveUserCommand : IRequest<bool>, IAuthorizedRequest
{
    public string UserId { get; set; } = string.Empty;
    public string? Reason { get; set; }

    public string RequiredPermission => AppPermissions.UsersManage;
    public IReadOnlyList<ResourceScope> Scopes => [];
}
