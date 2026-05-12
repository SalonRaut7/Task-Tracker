using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.DTOs;

public abstract class SprintBaseDto
{
    public string Name { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public SprintStatus Status { get; set; } = SprintStatus.Planning;
}

public sealed class SprintDto : SprintBaseDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? ArchiveReason { get; set; }
    public DateTime? ArchivedAtUTC { get; set; }
    public string? ArchivedByUserId { get; set; }
}

/// <summary>
/// Returned by CompleteSprintCommand — contains the completed sprint
/// and a summary of tasks rolled back to the backlog.
/// </summary>
public sealed class CompleteSprintResult
{
    public SprintDto Sprint { get; set; } = null!;

    /// <summary>Tasks that were not in a terminal state when the sprint was completed.</summary>
    public int IncompleteTaskCount { get; set; }

    /// <summary>Tasks whose SprintId was cleared and rolled back to the backlog.</summary>
    public int RolledOverTaskCount { get; set; }
}

