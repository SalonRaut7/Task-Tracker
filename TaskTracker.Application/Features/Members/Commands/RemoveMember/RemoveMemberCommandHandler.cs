using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Members.Commands.RemoveMember;

public sealed class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand, bool>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IMembershipRepository _membershipRepository;

    public RemoveMemberCommandHandler(
        ICurrentUserService currentUser,
        IMembershipRepository membershipRepository)
    {
        _currentUser = currentUser;
        _membershipRepository = membershipRepository;
    }

    public async Task<bool> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == _currentUser.UserId)
            throw new InvalidOperationException("You cannot remove yourself from a scope.");

        if (request.ScopeType == ScopeType.Organization)
        {
            var memberships = await _membershipRepository.GetOrganizationMembershipsAsync(request.ScopeId, cancellationToken);
            var targetRole = memberships.FirstOrDefault(m => m.UserId == request.UserId)?.Role;

            if (string.Equals(targetRole, AppRoles.OrgAdmin, StringComparison.Ordinal) && !_currentUser.IsSuperAdmin)
            {
                throw new ForbiddenAccessException("Only SuperAdmin can remove another OrgAdmin.");
            }
        }

        if (request.ScopeType == ScopeType.Organization)
        {
            await _membershipRepository.RemoveOrganizationMemberAsync(
                request.UserId, request.ScopeId, cancellationToken);
        }
        else
        {
            await _membershipRepository.RemoveProjectMemberAsync(
                request.UserId, request.ScopeId, cancellationToken);
        }

        return true;
    }
}