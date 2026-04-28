using TaskTracker.Domain.Entities.Identity;

namespace TaskTracker.Domain.Interfaces;

public interface IUserRepository
{
    Task<IReadOnlyList<UserSummaryReadModel>> GetUserSummariesAsync(
        bool archived,
        CancellationToken cancellationToken = default);

    Task<UserDetailsReadModel?> GetUserDetailsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<ApplicationUser?> GetByIdForUpdateAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task ArchiveAsync(
        ApplicationUser user,
        string archivedByUserId,
        string? archiveReason,
        CancellationToken cancellationToken = default);

    Task RestoreAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default);

    Task ReassignReportedTasksAsync(
        string sourceUserId,
        string targetUserId,
        CancellationToken cancellationToken = default);

    Task ClearTaskAssignmentsAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

public sealed class UserSummaryReadModel
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public bool IsSuperAdmin { get; init; }
    public bool IsActive { get; init; }
    public bool IsArchived { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? ArchivedAtUtc { get; init; }
    public string? ArchivedByUserId { get; init; }
    public string? ArchiveReason { get; init; }
    public int OrganizationCount { get; init; }
    public int ProjectCount { get; init; }
    public int AssignedTaskCount { get; init; }
    public int ReportedTaskCount { get; init; }
}

public sealed class UserDetailsReadModel
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsArchived { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? ArchivedAtUtc { get; init; }
    public string? ArchivedByUserId { get; init; }
    public string? ArchiveReason { get; init; }
    public int AssignedTaskCount { get; init; }
    public int ReportedTaskCount { get; init; }
    public IReadOnlyList<UserOrganizationMembershipReadModel> OrganizationMemberships { get; init; } = [];
    public IReadOnlyList<UserProjectMembershipReadModel> ProjectMemberships { get; init; } = [];
}

public sealed class UserOrganizationMembershipReadModel
{
    public Guid OrganizationId { get; init; }
    public string OrganizationName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime JoinedAt { get; init; }
}

public sealed class UserProjectMembershipReadModel
{
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public string OrganizationName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime JoinedAt { get; init; }
}
