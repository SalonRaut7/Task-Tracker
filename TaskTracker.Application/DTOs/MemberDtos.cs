using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.DTOs;

public sealed class MemberDto
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime JoinedAt { get; init; }
}

public sealed class ScopeMembersDto
{
    public ScopeType ScopeType { get; init; }
    public Guid ScopeId { get; init; }
    public IReadOnlyList<MemberDto> Members { get; init; } = [];
    public IReadOnlyList<InvitationDto> PendingInvitations { get; init; } = [];
    public IReadOnlyList<InvitableUserDto> InvitableUsers { get; init; } = [];
}

public sealed class InvitableUserDto
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
}

public sealed class UpdateMemberRoleDto
{
    public ScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string NewRole { get; set; } = string.Empty;
}
