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

  InvitationsCreate: "Invitations.Create",
  InvitationsView: "Invitations.View",
  InvitationsRevoke: "Invitations.Revoke",

  MembersView: "Members.View",
  MembersUpdateRole: "Members.UpdateRole",
  MembersRemove: "Members.Remove",
} as const;

export type AppPermission = (typeof AppPermissions)[keyof typeof AppPermissions];

const allPermissions = Object.values(AppPermissions);
const permissionSet = new Set<string>(allPermissions);

export function isAppPermission(value: string): value is AppPermission {
  return permissionSet.has(value);
}

/** Organization-scoped roles available for invite. */
export const organizationRoleOptions = [
  AppRoles.OrgAdmin,
  AppRoles.ProjectManager,
  AppRoles.Developer,
  AppRoles.QA,
  AppRoles.Viewer,
];

/** Project-scoped roles available for invite. */
export const projectRoleOptions = [
  AppRoles.ProjectManager,
  AppRoles.Developer,
  AppRoles.QA,
  AppRoles.Viewer,
];
