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

public sealed class CreateSprintDto : SprintBaseDto
{
    public Guid ProjectId { get; set; }
}

public sealed class UpdateSprintDto : SprintBaseDto;

public sealed class SprintDto : SprintBaseDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
