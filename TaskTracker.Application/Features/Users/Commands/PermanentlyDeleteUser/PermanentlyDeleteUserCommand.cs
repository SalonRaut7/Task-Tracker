using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Users.Commands.PermanentlyDeleteUser;

public sealed class PermanentlyDeleteUserCommand : IRequest<bool>, IAuthorizedRequest
{
    public string UserId { get; set; } = string.Empty;

    public string RequiredPermission => AppPermissions.UsersManage;
    public IReadOnlyList<ResourceScope> Scopes => [];
}
