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
        //No fields needed here yet, but this class is ready for future create-specific properties without affecting update DTOs
    }

    // For updating a task
    public class UpdateTaskDto : TaskBaseDto
    {
        // Only extra field specific to update
        public int? EndDateExtensionDays { get; set; }
    }

    // For returning task data to client
    public class TaskDto : TaskBaseDto
    {
        // Only extra fields specific to response
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}