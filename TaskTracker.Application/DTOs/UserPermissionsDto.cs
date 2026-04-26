namespace TaskTracker.Application.DTOs;

/// <summary>
/// Scope-qualified permissions returned by /api/me/permissions.
/// Permissions are NOT flat — each scope has its own role and permission set.
/// </summary>
public sealed class UserPermissionsDto
{
    public bool IsSuperAdmin { get; init; }
    public IReadOnlyList<OrganizationRoleDto> OrganizationRoles { get; init; } = [];
    public IReadOnlyList<ProjectRoleDto> ProjectRoles { get; init; } = [];
}

public sealed class OrganizationRoleDto
{
    public Guid OrganizationId { get; init; }
    public string OrganizationName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public IReadOnlyList<string> Permissions { get; init; } = [];
}

public sealed class ProjectRoleDto
{
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public string Role { get; init; } = string.Empty;
    public IReadOnlyList<string> Permissions { get; init; } = [];
}
