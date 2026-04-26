using TaskTracker.Domain.Entities;

namespace TaskTracker.Domain.Interfaces;

/// <summary>
/// Abstracts membership read/write operations so Application handlers
/// never resolve DbContext directly (eliminates Service Locator anti-pattern).
/// </summary>
public interface IMembershipRepository
{
    // ── Queries ──────────────────────────────────────────────

    /// Returns the list of organization IDs the user belongs to.
    Task<IReadOnlyList<Guid>> GetUserOrganizationIdsAsync(string userId, CancellationToken ct = default);

    /// Returns the list of project IDs the user belongs to.
    Task<IReadOnlyList<Guid>> GetUserProjectIdsAsync(string userId, CancellationToken ct = default);

    /// Returns true if the user is a member of the specified organization.
    Task<bool> IsOrganizationMemberAsync(string userId, Guid organizationId, CancellationToken ct = default);

    /// Returns organization memberships with user info for a given org.
    Task<List<UserOrganization>> GetOrganizationMembershipsAsync(Guid organizationId, CancellationToken ct = default);

    /// Returns project memberships with user info for a given project.
    Task<List<UserProject>> GetProjectMembershipsAsync(Guid projectId, CancellationToken ct = default);

    // ── Mutations ─────────────────────────────────────────────

    /// Creates or updates an organization membership (role upsert).
    Task UpsertOrganizationMemberAsync(string userId, Guid organizationId, string role, string? invitedByUserId, CancellationToken ct = default);

    /// Creates or updates a project membership (role upsert).
    /// Throws if the user is not a member of the project's parent org.
    Task UpsertProjectMemberAsync(string userId, Guid projectId, string role, string? invitedByUserId, CancellationToken ct = default);

    /// Updates the role of an existing organization member. Returns the updated membership.
    Task<UserOrganization> UpdateOrganizationMemberRoleAsync(string userId, Guid organizationId, string newRole, CancellationToken ct = default);

    /// Updates the role of an existing project member. Returns the updated membership.
    Task<UserProject> UpdateProjectMemberRoleAsync(string userId, Guid projectId, string newRole, CancellationToken ct = default);

    /// Removes a user from an organization. Cascades: also removes all project memberships in that org.
    Task RemoveOrganizationMemberAsync(string userId, Guid organizationId, CancellationToken ct = default);

    /// Removes a user from a project.
    Task RemoveProjectMemberAsync(string userId, Guid projectId, CancellationToken ct = default);
}
