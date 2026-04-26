using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Members.Queries.GetMyPermissions;

public sealed class GetMyPermissionsQuery : IRequest<UserPermissionsDto> { }

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
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new UnauthorizedAccessException("Authentication is required.");

        return await _permissionEvaluator.GetUserPermissionsAsync(_currentUser.UserId, cancellationToken);
    }
}
