namespace TaskTracker.Application.Options;

public class InviteOptions
{
    public const string SectionName = "Invite";

    /// Number of days before an invitation expires.
    public int ExpirationDays { get; set; }

    /// Base URL for the invitation acceptance page on the frontend.
    public string AcceptUrl { get; set; } = default!;
}
