namespace TaskTracker.Domain.Constants;

/// Defines all application roles
public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string OrgAdmin = "OrgAdmin";
    public const string ProjectManager = "ProjectManager";
    public const string Developer = "Developer";
    public const string QA = "QA";
    public const string Viewer = "Viewer";

    public static IReadOnlyList<string> All => new[]
    {
        SuperAdmin,
        OrgAdmin,
        ProjectManager,
        Developer,
        QA,
        Viewer
    };
}
