import { ApiError, apiRequest } from "./apiClient";
import type { BackendProject } from "../types/app";

interface ProjectListResult {
  items: BackendProject[];
  available: boolean;
  message?: string;
}

export async function getProjects(): Promise<ProjectListResult> {
  try {
    const raw = await apiRequest<unknown[]>("/api/Projects", {
      method: "GET",
      requiresAuth: true,
    });

    const items = (raw ?? [])
      .map((item) => normalizeProject(item))
      .filter((item): item is BackendProject => item !== null);

    return { items, available: true };
  } catch (error) {
    if (error instanceof ApiError && error.status === 403) {
      return {
        items: [],
        available: false,
        message: "You do not have permission to view projects.",
      };
    }

    if (error instanceof ApiError && error.status === 404) {
      return {
        items: [],
        available: false,
        message: "Projects endpoint is not available in the current backend.",
      };
    }

    throw error;
  }
}

export async function getProjectById(
  id: string
): Promise<{ item: BackendProject | null; available: boolean; message?: string }> {
  try {
    const raw = await apiRequest<unknown>(`/api/Projects/${id}`, {
      method: "GET",
      requiresAuth: true,
    });

    return { item: normalizeProject(raw), available: true };
  } catch (error) {
    if (error instanceof ApiError && error.status === 403) {
      return {
        item: null,
        available: false,
        message: "You do not have permission to view this project.",
      };
    }

    if (error instanceof ApiError && error.status === 404) {
      return {
        item: null,
        available: false,
        message: "Project details endpoint is not available in the current backend.",
      };
    }

    throw error;
  }
}

function normalizeProject(raw: unknown): BackendProject | null {
  if (!raw || typeof raw !== "object") {
    return null;
  }

  const item = raw as Record<string, unknown>;
  const id = String(item.id ?? item.Id ?? "");
  const name = String(item.name ?? item.Name ?? "");

  if (!id || !name) {
    return null;
  }

  return {
    id,
    name,
    key: item.key ? String(item.key) : item.Key ? String(item.Key) : undefined,
    description: item.description
      ? String(item.description)
      : item.Description
      ? String(item.Description)
      : undefined,
    status: item.status
      ? String(item.status)
      : item.Status
      ? String(item.Status)
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
