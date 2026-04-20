export const AppRoles = {
  SuperAdmin: "SuperAdmin",
  OrgAdmin: "OrgAdmin",
  ProjectManager: "ProjectManager",
  Developer: "Developer",
  QA: "QA",
  Viewer: "Viewer",
} as const;

export const AppPermissions = {
  UsersView: "Users.View",
  UsersManage: "Users.Manage",

  OrganizationsView: "Organizations.View",
  OrganizationsCreate: "Organizations.Create",
  OrganizationsUpdate: "Organizations.Update",
  OrganizationsDelete: "Organizations.Delete",

  ProjectsView: "Projects.View",
  ProjectsCreate: "Projects.Create",
  ProjectsUpdate: "Projects.Update",
  ProjectsDelete: "Projects.Delete",

  TasksView: "Tasks.View",
  TasksCreate: "Tasks.Create",
  TasksUpdate: "Tasks.Update",
  TasksDelete: "Tasks.Delete",
  TasksAssign: "Tasks.Assign",
  TasksChangeStatus: "Tasks.ChangeStatus",

  SprintsView: "Sprints.View",
  SprintsCreate: "Sprints.Create",
  SprintsManage: "Sprints.Manage",

  EpicsView: "Epics.View",
  EpicsCreate: "Epics.Create",
  EpicsUpdate: "Epics.Update",
  EpicsDelete: "Epics.Delete",

  CommentsView: "Comments.View",
  CommentsAdd: "Comments.Add",
  CommentsUpdate: "Comments.Update",
  CommentsDelete: "Comments.Delete",
} as const;

export type AppPermission = (typeof AppPermissions)[keyof typeof AppPermissions];

const allPermissions = Object.values(AppPermissions);
const permissionSet = new Set<string>(allPermissions);

export function isAppPermission(value: string): value is AppPermission {
  return permissionSet.has(value);
}

export function getPermissionsForRole(role: string): AppPermission[] {
  switch (role) {
    case AppRoles.SuperAdmin:
      return allPermissions;

    case AppRoles.OrgAdmin:
      return [
        AppPermissions.UsersView,
        AppPermissions.UsersManage,
        AppPermissions.OrganizationsView,
        AppPermissions.OrganizationsCreate,
        AppPermissions.OrganizationsUpdate,
        AppPermissions.OrganizationsDelete,
        AppPermissions.ProjectsView,
        AppPermissions.ProjectsCreate,
        AppPermissions.ProjectsUpdate,
        AppPermissions.ProjectsDelete,
        AppPermissions.TasksView,
        AppPermissions.TasksCreate,
        AppPermissions.TasksUpdate,
        AppPermissions.TasksDelete,
        AppPermissions.TasksAssign,
        AppPermissions.TasksChangeStatus,
        AppPermissions.SprintsView,
        AppPermissions.SprintsCreate,
        AppPermissions.SprintsManage,
        AppPermissions.EpicsView,
        AppPermissions.EpicsCreate,
        AppPermissions.EpicsUpdate,
        AppPermissions.EpicsDelete,
        AppPermissions.CommentsView,
        AppPermissions.CommentsAdd,
        AppPermissions.CommentsUpdate,
        AppPermissions.CommentsDelete,
      ];

    case AppRoles.ProjectManager:
      return [
        AppPermissions.UsersView,
        AppPermissions.OrganizationsView,
        AppPermissions.ProjectsView,
        AppPermissions.ProjectsCreate,
        AppPermissions.ProjectsUpdate,
        AppPermissions.TasksView,
        AppPermissions.TasksCreate,
        AppPermissions.TasksUpdate,
        AppPermissions.TasksDelete,
        AppPermissions.TasksAssign,
        AppPermissions.TasksChangeStatus,
        AppPermissions.SprintsView,
        AppPermissions.SprintsCreate,
        AppPermissions.SprintsManage,
        AppPermissions.EpicsView,
        AppPermissions.EpicsCreate,
        AppPermissions.EpicsUpdate,
        AppPermissions.EpicsDelete,
        AppPermissions.CommentsView,
        AppPermissions.CommentsAdd,
        AppPermissions.CommentsUpdate,
        AppPermissions.CommentsDelete,
      ];

    case AppRoles.Developer:
      return [
        AppPermissions.ProjectsView,
        AppPermissions.TasksView,
        AppPermissions.TasksCreate,
        AppPermissions.TasksUpdate,
        AppPermissions.TasksChangeStatus,
        AppPermissions.SprintsView,
        AppPermissions.EpicsView,
        AppPermissions.CommentsView,
        AppPermissions.CommentsAdd,
        AppPermissions.CommentsUpdate,
      ];

    case AppRoles.QA:
      return [
        AppPermissions.ProjectsView,
        AppPermissions.TasksView,
        AppPermissions.TasksChangeStatus,
        AppPermissions.SprintsView,
        AppPermissions.EpicsView,
        AppPermissions.CommentsView,
        AppPermissions.CommentsAdd,
      ];

    case AppRoles.Viewer:
      return [
        AppPermissions.OrganizationsView,
        AppPermissions.ProjectsView,
        AppPermissions.TasksView,
        AppPermissions.SprintsView,
        AppPermissions.EpicsView,
        AppPermissions.CommentsView,
      ];

    default:
      return [];
  }
}

export function getPermissionsForRoles(roles: readonly string[]): AppPermission[] {
  const permissions = new Set<AppPermission>();

  for (const role of roles) {
    for (const permission of getPermissionsForRole(role)) {
      permissions.add(permission);
    }
  }

  return Array.from(permissions);
}
