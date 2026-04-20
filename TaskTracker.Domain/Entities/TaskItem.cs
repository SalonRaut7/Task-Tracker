using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Entities.Identity;

namespace TaskTracker.Domain.Entities
{
    public class TaskItem
    {
        // Private setters — state can only be changed through domain methods
        public int Id { get; private set; }
        public Guid ProjectId { get; private set; }
        public Guid? EpicId { get; private set; }
        public Guid? SprintId { get; private set; }
        public string? AssigneeId { get; private set; }
        public string ReporterId { get; private set; } = string.Empty;
        public string Title { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public Status Status { get; private set; } = Status.NotStarted;
        public TaskPriority Priority { get; private set; } = TaskPriority.Medium;
        public DateOnly? StartDate { get; private set; }
        public DateOnly? EndDate { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public Project Project { get; private set; } = null!;
        public Epic? Epic { get; private set; }
        public Sprint? Sprint { get; private set; }
        public ApplicationUser? Assignee { get; private set; }
        public ApplicationUser Reporter { get; private set; } = null!;
        public ICollection<Comment> Comments { get; private set; } = new List<Comment>();

        // Required by EF Core — not for external use
        private TaskItem() { }

        public static TaskItem Create(
            Guid projectId,
            Guid? epicId,
            Guid? sprintId,
            string? assigneeId,
            string reporterId,
            string title,
            string? description,
            Status status,
            TaskPriority priority,
            DateOnly? startDate,
            DateOnly? endDate,
            DateTime utcNow)
        {
            var normalizedNow = NormalizeUtc(utcNow);

            if (string.IsNullOrWhiteSpace(reporterId))
                throw new InvalidOperationException("Reporter is required.");

            EnsureStartBeforeOrEqualEnd(startDate, endDate);

            return new TaskItem
            {
                ProjectId   = projectId,
                EpicId      = epicId,
                SprintId    = sprintId,
                AssigneeId  = string.IsNullOrWhiteSpace(assigneeId) ? null : assigneeId.Trim(),
                ReporterId  = reporterId.Trim(),
                Title       = title,
                Description = description,
                Status      = status,
                Priority    = priority,
                StartDate   = startDate,
                EndDate     = endDate,
                CreatedAt   = normalizedNow,
                UpdatedAt   = normalizedNow,
            };
        }

        public void ApplyUpdate(
            string title,
            string? description,
            Status status,
            TaskPriority priority,
            Guid? epicId,
            Guid? sprintId,
            string? assigneeId,
            DateOnly? startDate,
            DateOnly? endDate,
            int? endDateExtensionDays,
            IReadOnlySet<int> allowedExtensionDays,
            DateTime utcNow)
        {
            var normalizedNow = NormalizeUtc(utcNow);
            var today         = DateOnly.FromDateTime(normalizedNow);

            if (endDateExtensionDays.HasValue)
            {
                if (endDate.HasValue)
                    throw new InvalidOperationException(
                        "Provide either EndDate or EndDateExtensionDays, not both.");

                if (!allowedExtensionDays.Contains(endDateExtensionDays.Value))
                    throw new InvalidOperationException(
                        $"Invalid end date extension. Allowed values: {string.Join(", ", allowedExtensionDays)}.");

                if (!EndDate.HasValue)
                    throw new InvalidOperationException(
                        "End date extension is only allowed when the task already has an EndDate.");

                endDate = EndDate.Value.AddDays(endDateExtensionDays.Value);
            }

            EnforcePastDateChangeRules(startDate, endDate, today);
            EnsureStartBeforeOrEqualEnd(startDate, endDate);

            Title       = title;
            Description = description;
            Status      = status;
            Priority    = priority;
            EpicId      = epicId;
            SprintId    = sprintId;
            AssigneeId  = string.IsNullOrWhiteSpace(assigneeId) ? null : assigneeId.Trim();
            StartDate   = startDate;
            EndDate     = endDate;
            UpdatedAt   = normalizedNow;

            // Guard: domain ensures its own invariant after mutation
            if (CreatedAt > UpdatedAt)
                throw new InvalidOperationException(
                    "CreatedAt must be earlier than or equal to UpdatedAt.");
        }

        // Called by AppDbContext as a final consistency gate before persistence
        public void EnsureDateConsistency()
        {
            EnsureStartBeforeOrEqualEnd(StartDate, EndDate);

            if (CreatedAt > UpdatedAt)
                throw new InvalidOperationException(
                    "CreatedAt must be earlier than or equal to UpdatedAt.");
        }

        // ── Private helpers ────────────────────────────────────────────────

        private void EnforcePastDateChangeRules(
            DateOnly? newStartDate,
            DateOnly? newEndDate,
            DateOnly  today)
        {
            // StartDate in the past cannot be changed
            if (StartDate != newStartDate
                && newStartDate.HasValue
                && newStartDate.Value < today)
            {
                throw new InvalidOperationException(
                    "StartDate in the past cannot be modified.");
            }

            // EndDate unchanged — nothing to check
            if (EndDate == newEndDate) return;

            if (newEndDate.HasValue && newEndDate.Value < today)
            {
                // Allow only if we are extending a past EndDate forward
                var isExtendingPastEndDate =
                    EndDate.HasValue
                    && EndDate.Value < today
                    && newEndDate.Value > EndDate.Value;

                if (!isExtendingPastEndDate)
                    throw new InvalidOperationException(
                        "EndDate in the past cannot be modified unless it is being extended.");
            }
            else if (EndDate.HasValue
                     && EndDate.Value < today
                     && newEndDate.HasValue
                     && newEndDate.Value <= EndDate.Value)
            {
                throw new InvalidOperationException(
                    "Past EndDate can only be extended to a later date.");
            }
        }

        private static void EnsureStartBeforeOrEqualEnd(
            DateOnly? startDate,
            DateOnly? endDate)
        {
            if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
                throw new InvalidOperationException(
                    "StartDate must be earlier than or equal to EndDate.");
        }

        private static DateTime NormalizeUtc(DateTime dateTime) =>
            dateTime.Kind == DateTimeKind.Utc
                ? dateTime
                : dateTime.ToUniversalTime();
    }
}