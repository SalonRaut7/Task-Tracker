using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.DTOs
{
    public class CreateTaskDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Status Status { get; set; } = Status.NotStarted;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }
}
