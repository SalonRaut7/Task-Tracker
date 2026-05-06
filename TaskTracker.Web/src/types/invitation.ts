export type ScopeType = "Organization" | "Project";
export type ScopeTypeNumeric = 0 | 1;

export type InvitationStatus = "Pending" | "Accepted" | "Revoked" | "Expired";

export interface Invitation {
  id: string;
  scopeType: ScopeTypeNumeric;
  scopeId: string;
  inviteeEmail: string;
  role: string;
  status: InvitationStatus;
  invitedByUserId: string;
  invitedByName: string;
  createdAt: string;
  expiresAt: string;
  acceptedAt?: string;
  revokedAt?: string;
}

export interface CreateInvitationPayload {
  scopeType: ScopeTypeNumeric;
  scopeId: string;
  inviteeEmail: string;
  role: string;
}

export interface AcceptInvitationPayload {
  token: string;
}

export interface AcceptInvitationResult {
  success: boolean;
  message: string;
  scopeType?: ScopeTypeNumeric;
  scopeId?: string;
  role?: string;
}

export interface ScopeMember {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  joinedAt: string;
}

export interface InvitableUser {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
}

export interface MentionableUser {
  userId: string;
  firstName: string;
  lastName: string;
  role: string;
}

export interface ScopeMembersResponse {
  scopeType: ScopeTypeNumeric;
  scopeId: string;
  members: ScopeMember[];
  pendingInvitations: Invitation[];
  invitableUsers: InvitableUser[];
  mentionableUsers: MentionableUser[];
}

export interface UpdateMemberRolePayload {
  scopeType: ScopeTypeNumeric;
  scopeId: string;
  userId: string;
  newRole: string;
}

export interface OrganizationRoleInfo {
  organizationId: string;
  organizationName: string;
  role: string;
  permissions: string[];
}

export interface ProjectRoleInfo {
  projectId: string;
  projectName: string;
  organizationId: string;
  role: string;
  permissions: string[];
}

export interface UserPermissions {
  isSuperAdmin: boolean;
  organizationRoles: OrganizationRoleInfo[];
  projectRoles: ProjectRoleInfo[];
}
