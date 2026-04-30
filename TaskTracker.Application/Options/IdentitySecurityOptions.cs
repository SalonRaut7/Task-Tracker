namespace TaskTracker.Application.Options;

public sealed class IdentitySecurityOptions
{
    public const string SectionName = "IdentitySettings";

    public int LockoutMinutes { get; set; } = 15;
    public int MaxFailedAccessAttempts { get; set; } = 5;
}