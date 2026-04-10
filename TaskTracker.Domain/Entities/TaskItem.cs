using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Entities
{
    public class TaskItem
    {
        // Private setters — state can only be changed through domain methods
        public int Id { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public Status Status { get; private set; } = Status.NotStarted;
        public TaskPriority Priority { get; private set; } = TaskPriority.Medium;
        public DateOnly? StartDate { get; private set; }
        public DateOnly? EndDate { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        // Required by EF Core — not for external use
        private TaskItem() { }

        public static TaskItem Create(
            string title,
            string? description,
            Status status,
            TaskPriority priority,
            DateOnly? startDate,
            DateOnly? endDate,
            int defaultSprintDurationDays,
            DateTime utcNow)
        {
            var normalizedNow      = NormalizeUtc(utcNow);
            var effectiveStartDate = startDate ?? DateOnly.FromDateTime(normalizedNow);
            var effectiveEndDate   = endDate   ?? effectiveStartDate.AddDays(defaultSprintDurationDays);

            EnsureStartBeforeOrEqualEnd(effectiveStartDate, effectiveEndDate);

            return new TaskItem
            {
                Title       = title,
                Description = description,
                Status      = status,
                Priority    = priority,
                StartDate   = effectiveStartDate,
                EndDate     = effectiveEndDate,
                CreatedAt   = normalizedNow,
                UpdatedAt   = normalizedNow,
            };
        }

        public void ApplyUpdate(
            string title,
            string? description,
            Status status,
            TaskPriority priority,
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