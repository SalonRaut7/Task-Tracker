import { apiRequest } from "./apiClient";
import type {
  BackendUserDetails,
  BackendUserOrganizationSummary,
  BackendUserProjectSummary,
  BackendUserSummary,
} from "../types/app";

export async function getUsers(): Promise<BackendUserSummary[]> {
  const raw = await apiRequest<unknown[]>("/api/Users", {
    method: "GET",
    requiresAuth: true,
  });

  return (raw ?? [])
    .map(normalizeUserSummary)
    .filter((item): item is BackendUserSummary => item !== null);
}

export async function getArchivedUsers(): Promise<BackendUserSummary[]> {
  const raw = await apiRequest<unknown[]>("/api/Users/archive", {
    method: "GET",
    requiresAuth: true,
  });

  return (raw ?? [])
    .map(normalizeUserSummary)
    .filter((item): item is BackendUserSummary => item !== null);
}

export async function getUserDetails(userId: string): Promise<BackendUserDetails | null> {
  const raw = await apiRequest<unknown>(`/api/Users/${encodeURIComponent(userId)}`, {
    method: "GET",
    requiresAuth: true,
  });

  return normalizeUserDetails(raw);
}

export async function archiveUser(userId: string, reason?: string): Promise<void> {
  await apiRequest<void>(`/api/Users/${encodeURIComponent(userId)}/archive`, {
    method: "POST",
    requiresAuth: true,
    body: { reason },
  });
}

export async function restoreUser(userId: string): Promise<void> {
  await apiRequest<void>(`/api/Users/${encodeURIComponent(userId)}/restore`, {
    method: "POST",
    requiresAuth: true,
  });
}

export async function permanentlyDeleteUser(userId: string): Promise<void> {
  await apiRequest<void>(`/api/Users/${encodeURIComponent(userId)}/permanent`, {
    method: "DELETE",
    requiresAuth: true,
  });
}

function normalizeUserSummary(raw: unknown): BackendUserSummary | null {
  if (!raw || typeof raw !== "object") {
    return null;
  }

  const item = raw as Record<string, unknown>;
  const userId = toString(item.userId ?? item.UserId);
  const email = toString(item.email ?? item.Email);
  const firstName = toString(item.firstName ?? item.FirstName);
  const lastName = toString(item.lastName ?? item.LastName);

  if (!userId || !email || !firstName || !lastName) {
    return null;
  }

  return {
    userId,
    email,
    firstName,
    lastName,
    isSuperAdmin: toBoolean(item.isSuperAdmin ?? item.IsSuperAdmin),
    isActive: toBoolean(item.isActive ?? item.IsActive),
    isArchived: toBoolean(item.isArchived ?? item.IsArchived),
    createdAt: toString(item.createdAt ?? item.CreatedAt) || new Date(0).toISOString(),
    updatedAt: toString(item.updatedAt ?? item.UpdatedAt) || new Date(0).toISOString(),
    archivedAtUtc: toOptionalString(item.archivedAtUtc ?? item.ArchivedAtUtc),
    archivedByUserId: toOptionalString(item.archivedByUserId ?? item.ArchivedByUserId),
    archiveReason: toOptionalString(item.archiveReason ?? item.ArchiveReason),
    organizationCount: toNumber(item.organizationCount ?? item.OrganizationCount),
    projectCount: toNumber(item.projectCount ?? item.ProjectCount),
    assignedTaskCount: toNumber(item.assignedTaskCount ?? item.AssignedTaskCount),
    reportedTaskCount: toNumber(item.reportedTaskCount ?? item.ReportedTaskCount),
  };
}

function normalizeUserDetails(raw: unknown): BackendUserDetails | null {
  if (!raw || typeof raw !== "object") {
    return null;
  }

  const item = raw as Record<string, unknown>;
  const userId = toString(item.userId ?? item.UserId);
  const email = toString(item.email ?? item.Email);
  const firstName = toString(item.firstName ?? item.FirstName);
  const lastName = toString(item.lastName ?? item.LastName);

  if (!userId || !email || !firstName || !lastName) {
    return null;
  }

  const orgMembershipsSource = item.organizationMemberships ?? item.OrganizationMemberships;
  const orgMembershipsRaw: unknown[] = Array.isArray(orgMembershipsSource)
    ? orgMembershipsSource
    : [];

  const projectMembershipsSource = item.projectMemberships ?? item.ProjectMemberships;
  const projectMembershipsRaw: unknown[] = Array.isArray(projectMembershipsSource)
    ? projectMembershipsSource
    : [];

  return {
    userId,
    email,
    firstName,
    lastName,
    isActive: toBoolean(item.isActive ?? item.IsActive),
    isArchived: toBoolean(item.isArchived ?? item.IsArchived),
    createdAt: toString(item.createdAt ?? item.CreatedAt) || new Date(0).toISOString(),
    updatedAt: toString(item.updatedAt ?? item.UpdatedAt) || new Date(0).toISOString(),
    archivedAtUtc: toOptionalString(item.archivedAtUtc ?? item.ArchivedAtUtc),
    archivedByUserId: toOptionalString(item.archivedByUserId ?? item.ArchivedByUserId),
    archiveReason: toOptionalString(item.archiveReason ?? item.ArchiveReason),
    assignedTaskCount: toNumber(item.assignedTaskCount ?? item.AssignedTaskCount),
    reportedTaskCount: toNumber(item.reportedTaskCount ?? item.ReportedTaskCount),
    organizationMemberships: orgMembershipsRaw
      .map(normalizeOrganizationMembership)
      .filter((entry): entry is BackendUserOrganizationSummary => entry !== null),
    projectMemberships: projectMembershipsRaw
      .map(normalizeProjectMembership)
      .filter((entry): entry is BackendUserProjectSummary => entry !== null),
  };
}

function normalizeOrganizationMembership(raw: unknown): BackendUserOrganizationSummary | null {
  if (!raw || typeof raw !== "object") {
    return null;
  }

  const item = raw as Record<string, unknown>;
  const organizationId = toString(item.organizationId ?? item.OrganizationId);
  const organizationName = toString(item.organizationName ?? item.OrganizationName);
  const role = toString(item.role ?? item.Role);
  const joinedAt = toString(item.joinedAt ?? item.JoinedAt);

  if (!organizationId || !organizationName || !role || !joinedAt) {
    return null;
  }

  return {
    organizationId,
    organizationName,
    role,
    joinedAt,
  };
}

function normalizeProjectMembership(raw: unknown): BackendUserProjectSummary | null {
  if (!raw || typeof raw !== "object") {
    return null;
  }

  const item = raw as Record<string, unknown>;
  const projectId = toString(item.projectId ?? item.ProjectId);
  const projectName = toString(item.projectName ?? item.ProjectName);
  const organizationId = toString(item.organizationId ?? item.OrganizationId);
  const organizationName = toString(item.organizationName ?? item.OrganizationName);
  const role = toString(item.role ?? item.Role);
  const joinedAt = toString(item.joinedAt ?? item.JoinedAt);

  if (!projectId || !projectName || !organizationId || !organizationName || !role || !joinedAt) {
    return null;
  }

  return {
    projectId,
    projectName,
    organizationId,
    organizationName,
    role,
    joinedAt,
  };
}

function toString(value: unknown): string {
  return typeof value === "string" ? value : value == null ? "" : String(value);
}

function toOptionalString(value: unknown): string | undefined {
  const normalized = toString(value).trim();
  return normalized ? normalized : undefined;
}

function toBoolean(value: unknown): boolean {
  if (typeof value === "boolean") {
    return value;
  }

  if (typeof value === "string") {
    return value.toLowerCase() === "true";
  }

  if (typeof value === "number") {
    return value !== 0;
  }

  return false;
}

function toNumber(value: unknown): number {
  if (typeof value === "number") {
    return Number.isFinite(value) ? value : 0;
  }

  if (typeof value === "string") {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : 0;
  }

  return 0;
}
