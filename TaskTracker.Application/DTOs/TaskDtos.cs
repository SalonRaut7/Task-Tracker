using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.DTOs
{
    // Base shared properties
    public abstract class TaskBaseDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Status Status { get; set; } = Status.NotStarted;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }

    // For creating a task
    public class CreateTaskDto : TaskBaseDto
    {
        public Guid ProjectId { get; set; }
        public Guid? EpicId { get; set; }
        public Guid? SprintId { get; set; }
        public string? AssigneeId { get; set; }
    }

    // For updating a task
    public class UpdateTaskDto : TaskBaseDto
    {
        public Guid? EpicId { get; set; }
        public Guid? SprintId { get; set; }
        public string? AssigneeId { get; set; }
        public int? EndDateExtensionDays { get; set; }
    }

    // For returning task data to client
    public class TaskDto : TaskBaseDto
    {
        // Only extra fields specific to response
        public int Id { get; set; }
        public Guid ProjectId { get; set; }
        public Guid? EpicId { get; set; }
        public Guid? SprintId { get; set; }
        public string? AssigneeId { get; set; }
        public string ReporterId { get; set; } = string.Empty;
        public TaskUserIdentityDto? AssigneeUser { get; set; }
        public TaskUserIdentityDto? ReporterUser { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class TaskUserIdentityDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Role { get; set; }
        public bool IsActive { get; set; }
        public bool IsArchived { get; set; }
    }
}
