import { apiRequest } from "./apiClient";
import type { BackendSprint } from "../types/app";

export interface CreateSprintPayload {
  projectId: string;
  name: string;
  goal?: string;
  startDate: string;
  endDate: string;
  status: number;
}

export interface UpdateSprintPayload {
  name: string;
  goal?: string;
  startDate: string;
  endDate: string;
  status: number;
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
  };
}
