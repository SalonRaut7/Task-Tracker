using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.DTOs
{
    public class UpdateTaskDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Status Status { get; set; }
        public TaskPriority Priority { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int? EndDateExtensionDays { get; set; }
    }
}