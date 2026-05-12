using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Entities;

/// Time-boxed iteration for organizing work.
public class Sprint
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Goal { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public SprintStatus Status { get; private set; } = SprintStatus.Planning;
    public string? ArchiveReason { get; private set; }
    public DateTime? ArchivedAtUTC { get; private set; }
    public string? ArchivedByUserId { get; set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Project Project { get; private set; } = null!;
    public ICollection<TaskItem> Tasks { get; private set; } = new List<TaskItem>();

    private Sprint() { }

    public static Sprint Create(
        Guid projectId,
        string name,
        string? goal,
        DateOnly startDate,
        DateOnly endDate,
        DateTime utcNow)
    {
        var now = NormalizeUtc(utcNow);

        EnsureStartBeforeOrEqualEnd(startDate, endDate);
        EnsureMinimumDuration(startDate, endDate);

        return new Sprint
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = name.Trim(),
            Goal = string.IsNullOrWhiteSpace(goal) ? null : goal.Trim(),
            StartDate = startDate,
            EndDate = endDate,
            Status = SprintStatus.Planning,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void ApplyUpdate(
        string name,
        string? goal,
        DateOnly startDate,
        DateOnly endDate,
        DateTime utcNow)
    {
        if (Status is SprintStatus.Completed or SprintStatus.Cancelled or SprintStatus.Archived)
            throw new InvalidOperationException(
                $"Cannot edit a sprint that is {Status}.");

        if (Status == SprintStatus.Active && startDate != StartDate)
            throw new InvalidOperationException(
                "StartDate cannot be changed once the sprint is Active.");

        EnsureStartBeforeOrEqualEnd(startDate, endDate);
        EnsureMinimumDuration(startDate, endDate);

        Name = name.Trim();
        Goal = string.IsNullOrWhiteSpace(goal) ? null : goal.Trim();
        StartDate = startDate;
        EndDate = endDate;
        UpdatedAt = NormalizeUtc(utcNow);
    }

    public void TransitionTo(SprintStatus newStatus, DateOnly today, DateTime utcNow, string? archiveReason = null, string? archivedByUserId = null)
    {
        var allowed = Status switch
        {
            SprintStatus.Planning => new[] { SprintStatus.Active, SprintStatus.Cancelled },
            SprintStatus.Active => new[] { SprintStatus.Completed, SprintStatus.Cancelled },
            SprintStatus.Completed => new[] { SprintStatus.Archived },
            SprintStatus.Cancelled => new[] { SprintStatus.Archived },
            SprintStatus.Archived => Array.Empty<SprintStatus>(),
            _ => Array.Empty<SprintStatus>(),
        };

        if (!allowed.Contains(newStatus))
            throw new InvalidOperationException(
                $"Cannot transition sprint from {Status} to {newStatus}. " +
                $"Allowed: {(allowed.Length == 0 ? "none" : string.Join(", ", allowed))}.");
        
        if(newStatus == SprintStatus.Archived)
        {
            if(string.IsNullOrWhiteSpace(archiveReason))
                throw new InvalidOperationException("Archive reason must be provided when archiving a sprint.");
            
            if(archivedByUserId == null)
                throw new InvalidOperationException("A user ID is required when archiving a sprint.");
            ArchiveReason = archiveReason.Trim();
            ArchivedAtUTC = NormalizeUtc(utcNow);
            ArchivedByUserId = archivedByUserId;
        }

        Status = newStatus;
        UpdatedAt = NormalizeUtc(utcNow);
    }

    /// Called by AppDbContext right before persistence.
    /// Guarantees date invariants can never leak to the DB.
    public void EnsureDateConsistency()
    {
        EnsureStartBeforeOrEqualEnd(StartDate, EndDate);

        if (CreatedAt > UpdatedAt)
            throw new InvalidOperationException(
                "Sprint CreatedAt must be earlier than or equal to UpdatedAt.");

        if(Status == SprintStatus.Archived && (ArchiveReason is null || ArchivedAtUTC is null || ArchivedByUserId is null) )
        {
            throw new InvalidOperationException("Archived sprint must have ArchiveReason, ArchivedAtUtc, and ArchivedByUserId set.");
        }
    }

    private static void EnsureStartBeforeOrEqualEnd(DateOnly startDate, DateOnly endDate)
    {
        if (startDate > endDate)
            throw new InvalidOperationException(
                "Sprint StartDate must be earlier than or equal to EndDate.");
    }

    private static void EnsureMinimumDuration(DateOnly startDate, DateOnly endDate)
    {
        if (endDate == startDate)
            throw new InvalidOperationException(
                "Sprint must last at least 1 day (EndDate must be after StartDate).");
    }

    private static DateTime NormalizeUtc(DateTime dt) =>
        dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
}
