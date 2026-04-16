namespace TaskTracker.Application.Options;

public class OtpOptions
{
    public const string SectionName = "OtpSettings";

    public int ExpiryMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = 5;
    public int MaxResends { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 30;
    public string HashKey { get; set; } = string.Empty;
}
