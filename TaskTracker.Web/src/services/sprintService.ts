import { apiRequest } from "./apiClient";
import type { BackendSprint } from "../types/app";

export interface CreateSprintPayload {
  projectId: string;
  name: string;
  goal?: string;
  startDate: string;
  endDate: string;
}

export interface UpdateSprintPayload {
  name: string;
  goal?: string;
  startDate: string;
  endDate: string;
}

export async function getSprints(projectId?: string): Promise<BackendSprint[]> {
  const query = projectId ? `?projectId=${encodeURIComponent(projectId)}` : "";
  const raw = await apiRequest<unknown[]>(`/api/Sprints${query}`, {
    method: "GET",
    requiresAuth: true,
  });

  return (raw ?? [])
    .map(normalizeSprint)
    .filter((item): item is BackendSprint => item !== null);
}

export async function createSprint(payload: CreateSprintPayload): Promise<BackendSprint> {
  const raw = await apiRequest<unknown>("/api/Sprints", {
    method: "POST",
    requiresAuth: true,
    body: payload,
  });

  const sprint = normalizeSprint(raw);
  if (!sprint) {
    throw new Error("Invalid sprint payload returned by backend.");
  }

  return sprint;
}

export async function getSprintById(id: string): Promise<BackendSprint | null> {
  const raw = await apiRequest<unknown>(`/api/Sprints/${id}`, {
    method: "GET",
    requiresAuth: true,
  });

  return normalizeSprint(raw);
}

export async function updateSprint(
  id: string,
  payload: UpdateSprintPayload
): Promise<BackendSprint> {
  const raw = await apiRequest<unknown>(`/api/Sprints/${id}`, {
    method: "PUT",
    requiresAuth: true,
    body: payload,
  });

  const sprint = normalizeSprint(raw);
  if (!sprint) {
    throw new Error("Invalid sprint payload returned by backend.");
  }

  return sprint;
}

export async function deleteSprint(id: string): Promise<void> {
  await apiRequest<void>(`/api/Sprints/${id}`, {
    method: "DELETE",
    requiresAuth: true,
  });
}

export async function startSprint(id: string): Promise<BackendSprint> {
  const raw = await apiRequest<unknown>(`/api/Sprints/${id}/start`, {
    method: "POST",
    requiresAuth: true,
  });
  const sprint = normalizeSprint(raw);
  if (!sprint) throw new Error("Invalid sprint payload returned by backend.");
  return sprint;
}

export async function completeSprint(
  id: string
): Promise<{ sprint: BackendSprint; incompleteTaskCount: number; rolledOverTaskCount: number }> {
  const raw = await apiRequest<Record<string, unknown>>(`/api/Sprints/${id}/complete`, {
    method: "POST",
    requiresAuth: true,
  });
  if (!raw || typeof raw !== "object") throw new Error("Invalid response from server.");
  const sprint = normalizeSprint(raw.sprint ?? raw.Sprint);
  if (!sprint) throw new Error("Invalid sprint in complete response.");
  return {
    sprint,
    incompleteTaskCount: Number(raw.incompleteTaskCount ?? raw.IncompleteTaskCount ?? 0),
    rolledOverTaskCount: Number(raw.rolledOverTaskCount ?? raw.RolledOverTaskCount ?? 0),
  };
}

export async function cancelSprint(id: string): Promise<BackendSprint> {
  const raw = await apiRequest<unknown>(`/api/Sprints/${id}/cancel`, {
    method: "POST",
    requiresAuth: true,
  });
  const sprint = normalizeSprint(raw);
  if (!sprint) throw new Error("Invalid sprint payload returned by backend.");
  return sprint;
}

export async function archiveSprint(id: string, reason: string): Promise<BackendSprint> {
  const raw = await apiRequest<unknown>(`/api/Sprints/${id}/archive`, {
    method: "POST",
    requiresAuth: true,
    body: { archiveReason: reason },
  });
  const sprint = normalizeSprint(raw);
  if (!sprint) throw new Error("Invalid sprint payload returned by backend.");
  return sprint;
}


function normalizeSprint(raw: unknown): BackendSprint | null {
  if (!raw || typeof raw !== "object") {
    return null;
  }

  const item = raw as Record<string, unknown>;
  const id = String(item.id ?? item.Id ?? "");
  const projectId = String(item.projectId ?? item.ProjectId ?? "");
  const name = String(item.name ?? item.Name ?? "");
  const startDate = String(item.startDate ?? item.StartDate ?? "");
  const endDate = String(item.endDate ?? item.EndDate ?? "");
  const statusValue = item.status ?? item.Status;
  const status = typeof statusValue === "number" ? statusValue : Number(statusValue ?? 0);

  if (!id || !projectId || !name || !startDate || !endDate || Number.isNaN(status)) {
    return null;
  }

  return {
    id,
    projectId,
    name,
    startDate,
    endDate,
    status,
    goal: item.goal ? String(item.goal) : item.Goal ? String(item.Goal) : undefined,
    createdAt: item.createdAt
      ? String(item.createdAt)
      : item.CreatedAt
      ? String(item.CreatedAt)
      : undefined,
    updatedAt: item.updatedAt
      ? String(item.updatedAt)
      : item.UpdatedAt
      ? String(item.UpdatedAt)
      : undefined,
    archiveReason: item.archiveReason
      ? String(item.archiveReason)
      : item.ArchiveReason
      ? String(item.ArchiveReason)
      : undefined,
    archivedAtUTC: item.archivedAtUTC
      ? String(item.archivedAtUTC)
      : item.ArchivedAtUTC
      ? String(item.ArchivedAtUTC)
      : undefined,
    archivedByUserId: item.archivedByUserId
      ? String(item.archivedByUserId)
      : item.ArchivedByUserId
      ? String(item.ArchivedByUserId)
      : undefined,
  };
}
