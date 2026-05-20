namespace TaskTracker.Application.Constants;

// Strongly-typed cache key builders. All keys are prefixed with "cache:"
// to namespace them cleanly in logs and potential future Redis migration.

public static class CacheKeys
{
    // Caches (IsActive, IsArchived) tuple — checked on every request in OnTokenValidated
    public static string UserStatus(string userId) => $"cache:user-status:{userId}";

    // Caches whether a user is a global SuperAdmin — checked in PermissionEvaluator on every authorized request
    public static string UserIsSuperAdmin(string userId) => $"cache:user-is-superadmin:{userId}";

    // Caches the full UserPermissionsDto bundle (all org + project roles + permissions).
    public static string UserPermissions(string userId) => $"cache:user-permissions:{userId}";

    // Prefix for all permission-related keys for a user. Used for bulk invalidation
    public static string UserPermissionsPrefix(string userId) => $"cache:user-permissions:{userId}";

    // Caches the list of OrganizationIds a user belongs to — used in GetAllTasks, GetProjects scoping
    public static string UserOrgIds(string userId) => $"cache:user-org-ids:{userId}";

    // Caches the list of ProjectIds a user belongs to — used in GetAllTasks scoping
    public static string UserProjectIds(string userId) => $"cache:user-proj-ids:{userId}";

    // Caches user's role in a specific organization
    public static string UserOrgRole(string userId, Guid orgId) => $"cache:user-org-role:{userId}:{orgId}";

    // Caches user's role in a specific project.
    public static string UserProjectRole(string userId, Guid projectId) => $"cache:user-proj-role:{userId}:{projectId}";

    // Caches task→projectId mapping for AuthorizationScopeResolver. Immutable after task creation.
    public static string TaskProject(int taskId) => $"cache:task-project:{taskId}";

    // Caches epic→projectId mapping for AuthorizationScopeResolver.
    public static string EpicProject(Guid epicId) => $"cache:epic-project:{epicId}";

    // Caches sprint→projectId mapping for AuthorizationScopeResolver.
    public static string SprintProject(Guid sprintId) => $"cache:sprint-project:{sprintId}";

    // Caches comment→projectId mapping for AuthorizationScopeResolver.
    public static string CommentProject(Guid commentId) => $"cache:comment-project:{commentId}";

    // Caches lightweight project data (id, name, key, orgId) for validation lookups.
    public static string Project(Guid projectId) => $"cache:project:{projectId}";

    // Prefixes for bulk invalidation 
    public const string UserPrefix = "cache:user-";
    public const string ResourceScopePrefix = "cache:task-project:";
    public const string EpicScopePrefix = "cache:epic-project:";
    public const string SprintScopePrefix = "cache:sprint-project:";
    public const string CommentScopePrefix = "cache:comment-project:";
    public const string ProjectPrefix = "cache:project:";

    // All user-scoped cache key prefixes for a given userId — for bulk user invalidation
    public static string[] AllUserPrefixes(string userId) =>
    [
        UserStatus(userId),
        UserIsSuperAdmin(userId),
        UserPermissions(userId),
        UserOrgIds(userId),
        UserProjectIds(userId),
        $"cache:user-org-role:{userId}:",
        $"cache:user-proj-role:{userId}:",
    ];
}
