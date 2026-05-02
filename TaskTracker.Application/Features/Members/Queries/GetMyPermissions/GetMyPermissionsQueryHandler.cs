using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Members.Queries.GetMyPermissions;

public sealed class GetMyPermissionsQueryHandler : IRequestHandler<GetMyPermissionsQuery, UserPermissionsDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public GetMyPermissionsQueryHandler(
        ICurrentUserService currentUser,
        IPermissionEvaluator permissionEvaluator)
    {
        _currentUser = currentUser;
        _permissionEvaluator = permissionEvaluator;
    }

    public async Task<UserPermissionsDto> Handle(GetMyPermissionsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        return await _permissionEvaluator.GetUserPermissionsAsync(userId, cancellationToken);
    }
}
