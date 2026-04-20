import { apiRequest } from "./apiClient";
import type { BackendEpic } from "../types/app";

export interface CreateEpicPayload {
  projectId: string;
  title: string;
  description?: string;
  status: number;
}

export interface UpdateEpicPayload {
  title: string;
  description?: string;
  status: number;
}

export async function getEpics(projectId?: string): Promise<BackendEpic[]> {
  const query = projectId ? `?projectId=${encodeURIComponent(projectId)}` : "";
  const raw = await apiRequest<unknown[]>(`/api/Epics${query}`, {
    method: "GET",
    requiresAuth: true,
  });

  return (raw ?? []).map(normalizeEpic).filter((item): item is BackendEpic => item !== null);
}

export async function createEpic(payload: CreateEpicPayload): Promise<BackendEpic> {
  const raw = await apiRequest<unknown>("/api/Epics", {
    method: "POST",
    requiresAuth: true,
    body: payload,
  });

  const epic = normalizeEpic(raw);
  if (!epic) {
    throw new Error("Invalid epic payload returned by backend.");
  }

  return epic;
}

export async function getEpicById(id: string): Promise<BackendEpic | null> {
  const raw = await apiRequest<unknown>(`/api/Epics/${id}`, {
    method: "GET",
    requiresAuth: true,
  });

  return normalizeEpic(raw);
}

export async function updateEpic(id: string, payload: UpdateEpicPayload): Promise<BackendEpic> {
  const raw = await apiRequest<unknown>(`/api/Epics/${id}`, {
    method: "PUT",
    requiresAuth: true,
    body: payload,
  });

  const epic = normalizeEpic(raw);
  if (!epic) {
    throw new Error("Invalid epic payload returned by backend.");
  }

  return epic;
}

export async function deleteEpic(id: string): Promise<void> {
  await apiRequest<void>(`/api/Epics/${id}`, {
    method: "DELETE",
    requiresAuth: true,
  });
}

function normalizeEpic(raw: unknown): BackendEpic | null {
  if (!raw || typeof raw !== "object") {
    return null;
  }

  const item = raw as Record<string, unknown>;
  const id = String(item.id ?? item.Id ?? "");
  const projectId = String(item.projectId ?? item.ProjectId ?? "");
  const title = String(item.title ?? item.Title ?? "");
  const statusValue = item.status ?? item.Status;
  const status = typeof statusValue === "number" ? statusValue : Number(statusValue ?? 0);

  if (!id || !projectId || !title || Number.isNaN(status)) {
    return null;
  }

  return {
    id,
    projectId,
    title,
    status,
    description: item.description
      ? String(item.description)
      : item.Description
      ? String(item.Description)
      : undefined,
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
