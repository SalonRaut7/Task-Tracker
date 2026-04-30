namespace TaskTracker.Application.Options;

public sealed class NotificationOptions
{
    public const string SectionName = "NotificationSettings";

    public int DueSoonWindowHours { get; set; } = 24;
    public int DueDateMonitorIntervalMinutes { get; set; } = 15;
    public int RetentionCountPerUser { get; set; } = 100;
}