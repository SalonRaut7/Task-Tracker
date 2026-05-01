import type { BackendProject } from "../types/app";
import { ApiError, apiRequest } from "./apiClient";
import {
  appendPagingParams,
  normalizePagedResponse,
  type PagedResponse,
  type PagingOptions,
} from "./pagedResponse";

export interface CreateProjectPayload {
  organizationId: string;
  name: string;
  key: string;
  description?: string;
}

export interface UpdateProjectPayload {
  name: string;
  key: string;
  description?: string;
}

export interface ProjectQueryOptions extends PagingOptions {
  organizationId?: string;
  search?: string;
}

interface ProjectListResult extends PagedResponse<BackendProject> {
  available: boolean;
  message?: string;
}

export async function getProjects(
  options: ProjectQueryOptions = {}
): Promise<ProjectListResult> {
  try {
    const params = new URLSearchParams();

    if (options.organizationId?.trim()) {
      params.set("OrganizationId", options.organizationId.trim());
    }

    if (options.search?.trim()) {
      params.set("Search", options.search.trim());
    }

    appendPagingParams(params, options);

    const endpoint = params.toString()
      ? `/api/Projects?${params.toString()}`
      : "/api/Projects";

    const raw = await apiRequest<unknown>(endpoint, {
      method: "GET",
      requiresAuth: true,
    });

    const paged = normalizePagedResponse(raw, normalizeProject);

    return {
      ...paged,
      available: true,
    };
  } catch (error) {
    if (error instanceof ApiError && error.status === 403) {
      return {
        data: [],
        totalCount: 0,
        available: false,
        message: "You do not have permission to view projects.",
      };
    }

    if (error instanceof ApiError && error.status === 404) {
      return {
        data: [],
        totalCount: 0,
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

export async function createProject(payload: CreateProjectPayload): Promise<BackendProject> {
  const raw = await apiRequest<unknown>("/api/Projects", {
    method: "POST",
    requiresAuth: true,
    body: payload,
  });

  const project = normalizeProject(raw);
  if (!project) {
    throw new Error("Invalid project payload returned by backend.");
  }

  return project;
}

export async function updateProject(
  id: string,
  payload: UpdateProjectPayload
): Promise<BackendProject> {
  const raw = await apiRequest<unknown>(`/api/Projects/${id}`, {
    method: "PUT",
    requiresAuth: true,
    body: payload,
  });

  const project = normalizeProject(raw);
  if (!project) {
    throw new Error("Invalid project payload returned by backend.");
  }

  return project;
}

export async function deleteProject(id: string): Promise<void> {
  await apiRequest<void>(`/api/Projects/${id}`, {
    method: "DELETE",
    requiresAuth: true,
  });
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
    organizationId: item.organizationId
      ? String(item.organizationId)
      : item.OrganizationId
      ? String(item.OrganizationId)
      : undefined,
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
