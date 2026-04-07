using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Entities
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Status Status { get; set; } = Status.NotStarted;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    
}