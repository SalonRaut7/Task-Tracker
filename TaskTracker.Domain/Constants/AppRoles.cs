using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Constants;

/// Defines all application roles and scope-validation helpers.
public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string OrgAdmin = "OrgAdmin";
    public const string ProjectManager = "ProjectManager";
    public const string Developer = "Developer";
    public const string QA = "QA";
    public const string Viewer = "Viewer";

    /// All roles in the system (including global SuperAdmin).
    public static IReadOnlyList<string> All => new[]
    {
        SuperAdmin,
        OrgAdmin,
        ProjectManager,
        Developer,
        QA,
        Viewer
    };

    /// Roles that can be assigned at the organization scope.
    public static IReadOnlyList<string> OrganizationRoles => new[]
    {
        OrgAdmin,
        ProjectManager,
        Developer,
        QA,
        Viewer
    };

    /// Roles that can be assigned at the project scope.
    public static IReadOnlyList<string> ProjectRoles => new[]
    {
        ProjectManager,
        Developer,
        QA,
        Viewer
    };

    /// Validates whether a role is valid for a given scope type.
    public static bool IsValidForScope(string role, ScopeType scope) => scope switch
    {
        ScopeType.Organization => OrganizationRoles.Contains(role),
        ScopeType.Project => ProjectRoles.Contains(role),
        _ => false
    };
}
