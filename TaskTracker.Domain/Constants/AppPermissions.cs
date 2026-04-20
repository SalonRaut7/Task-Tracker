namespace TaskTracker.Domain.Constants;

/// Defines all granular permissions and their role mappings.
/// Used by the policy-based authorization system.
public static class AppPermissions
{
    // ── Users ────────────────────────────────────────────────
    public const string UsersView = "Users.View";
    public const string UsersManage = "Users.Manage";

    // ── Organizations ────────────────────────────────────────
    public const string OrganizationsView = "Organizations.View";
    public const string OrganizationsCreate = "Organizations.Create";
    public const string OrganizationsUpdate = "Organizations.Update";
    public const string OrganizationsDelete = "Organizations.Delete";

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

    // ── Epics ────────────────────────────────────────────────
    public const string EpicsView = "Epics.View";
    public const string EpicsCreate = "Epics.Create";
    public const string EpicsUpdate = "Epics.Update";
    public const string EpicsDelete = "Epics.Delete";

    // ── Comments ─────────────────────────────────────────────
    public const string CommentsView = "Comments.View";
    public const string CommentsAdd = "Comments.Add";
    public const string CommentsUpdate = "Comments.Update";
    public const string CommentsDelete = "Comments.Delete";

    /// Returns every permission string defined in the system
    public static IReadOnlyList<string> GetAllPermissions() => new[]
    {
        UsersView, UsersManage,
        OrganizationsView, OrganizationsCreate, OrganizationsUpdate, OrganizationsDelete,
        ProjectsView, ProjectsCreate, ProjectsUpdate, ProjectsDelete,
        TasksView, TasksCreate, TasksUpdate, TasksDelete, TasksAssign, TasksChangeStatus,
        SprintsView, SprintsCreate, SprintsManage,
        EpicsView, EpicsCreate, EpicsUpdate, EpicsDelete,
        CommentsView, CommentsAdd, CommentsUpdate, CommentsDelete
    };

    /// Returns the permissions granted to a given role.
    
    public static IReadOnlyList<string> GetPermissionsForRole(string role) => role switch
    {
        AppRoles.SuperAdmin => GetAllPermissions(),

        AppRoles.OrgAdmin => new[]
        {
            UsersView, UsersManage,
            OrganizationsView, OrganizationsCreate, OrganizationsUpdate, OrganizationsDelete,
            ProjectsView, ProjectsCreate, ProjectsUpdate, ProjectsDelete,
            TasksView, TasksCreate, TasksUpdate, TasksDelete, TasksAssign, TasksChangeStatus,
            SprintsView, SprintsCreate, SprintsManage,
            EpicsView, EpicsCreate, EpicsUpdate, EpicsDelete,
            CommentsView, CommentsAdd, CommentsUpdate, CommentsDelete
        },

        AppRoles.ProjectManager => new[]
        {
            UsersView,
            OrganizationsView,
            ProjectsView, ProjectsCreate, ProjectsUpdate,
            TasksView, TasksCreate, TasksUpdate, TasksDelete, TasksAssign, TasksChangeStatus,
            SprintsView, SprintsCreate, SprintsManage,
            EpicsView, EpicsCreate, EpicsUpdate, EpicsDelete,
            CommentsView, CommentsAdd, CommentsUpdate, CommentsDelete
        },

        AppRoles.Developer => new[]
        {
            ProjectsView,
            TasksView, TasksCreate, TasksUpdate, TasksChangeStatus,
            SprintsView,
            EpicsView,
            CommentsView, CommentsAdd, CommentsUpdate
        },

        AppRoles.QA => new[]
        {
            ProjectsView,
            TasksView, TasksChangeStatus,
            SprintsView,
            EpicsView,
            CommentsView, CommentsAdd
        },

        AppRoles.Viewer => new[]
        {
            OrganizationsView,
            ProjectsView,
            TasksView,
            SprintsView,
            EpicsView,
            CommentsView
        },

        _ => Array.Empty<string>()
    };
}
