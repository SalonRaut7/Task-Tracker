namespace TaskTracker.Domain.Constants;

/// Defines all granular permissions and their role mappings.
/// Used by the policy-based authorization system.
public static class AppPermissions
{
    // ── Users ────────────────────────────────────────────────
    public const string UsersView = "Users.View";
    public const string UsersManage = "Users.Manage";

    // ── Projects ─────────────────────────────────────────────
    public const string ProjectsView = "Projects.View";
    public const string ProjectsCreate = "Projects.Create";
    public const string ProjectsUpdate = "Projects.Update";
    public const string ProjectsDelete = "Projects.Delete";

    // ── Tasks ────────────────────────────────────────────────
    public const string TasksView = "Tasks.View";
    public const string TasksCreate = "Tasks.Create";
    public const string TasksUpdate = "Tasks.Update";
    public const string TasksDelete = "Tasks.Delete";
    public const string TasksAssign = "Tasks.Assign";
    public const string TasksChangeStatus = "Tasks.ChangeStatus";

    // ── Sprints ──────────────────────────────────────────────
    public const string SprintsView = "Sprints.View";
    public const string SprintsCreate = "Sprints.Create";
    public const string SprintsManage = "Sprints.Manage";

    // ── Comments ─────────────────────────────────────────────
    public const string CommentsAdd = "Comments.Add";
    public const string CommentsDelete = "Comments.Delete";

    /// Returns every permission string defined in the system
    public static IReadOnlyList<string> GetAllPermissions() => new[]
    {
        UsersView, UsersManage,
        ProjectsView, ProjectsCreate, ProjectsUpdate, ProjectsDelete,
        TasksView, TasksCreate, TasksUpdate, TasksDelete, TasksAssign, TasksChangeStatus,
        SprintsView, SprintsCreate, SprintsManage,
        CommentsAdd, CommentsDelete
    };

    /// Returns the permissions granted to a given role.
    
    public static IReadOnlyList<string> GetPermissionsForRole(string role) => role switch
    {
        AppRoles.SuperAdmin => GetAllPermissions(),

        AppRoles.OrgAdmin => new[]
        {
            UsersView, UsersManage,
            ProjectsView, ProjectsCreate, ProjectsUpdate, ProjectsDelete,
            TasksView, TasksCreate, TasksUpdate, TasksDelete, TasksAssign, TasksChangeStatus,
            SprintsView, SprintsCreate, SprintsManage,
            CommentsAdd, CommentsDelete
        },

        AppRoles.ProjectManager => new[]
        {
            UsersView,
            ProjectsView, ProjectsCreate, ProjectsUpdate,
            TasksView, TasksCreate, TasksUpdate, TasksDelete, TasksAssign, TasksChangeStatus,
            SprintsView, SprintsCreate, SprintsManage,
            CommentsAdd, CommentsDelete
        },

        AppRoles.Developer => new[]
        {
            ProjectsView,
            TasksView, TasksCreate, TasksUpdate, TasksChangeStatus,
            SprintsView,
            CommentsAdd
        },

        AppRoles.QA => new[]
        {
            ProjectsView,
            TasksView, TasksChangeStatus,
            SprintsView,
            CommentsAdd
        },

        AppRoles.Viewer => new[]
        {
            ProjectsView,
            TasksView,
            SprintsView
        },

        _ => Array.Empty<string>()
    };
}
