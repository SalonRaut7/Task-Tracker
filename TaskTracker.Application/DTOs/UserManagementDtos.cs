namespace TaskTracker.Application.DTOs;

public sealed class UserSummaryDto
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

public sealed class UserDetailsDto
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
    public IReadOnlyList<UserOrganizationSummaryDto> OrganizationMemberships { get; init; } = [];
    public IReadOnlyList<UserProjectSummaryDto> ProjectMemberships { get; init; } = [];
}

public sealed class UserOrganizationSummaryDto
{
    public Guid OrganizationId { get; init; }
    public string OrganizationName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime JoinedAt { get; init; }
}

public sealed class UserProjectSummaryDto
{
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public string OrganizationName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime JoinedAt { get; init; }
}

public sealed class ArchiveUserDto
{
    public string? Reason { get; set; }
}
