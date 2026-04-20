import { apiRequest } from "./apiClient";
import type { BackendOrganization } from "../types/app";

export interface CreateOrganizationPayload {
  name: string;
  slug: string;
  description?: string;
}

export interface UpdateOrganizationPayload {
  name: string;
  slug: string;
  description?: string;
}

export async function getOrganizations(): Promise<BackendOrganization[]> {
  const raw = await apiRequest<unknown[]>("/api/Organizations", {
    method: "GET",
    requiresAuth: true,
  });

  return (raw ?? [])
    .map(normalizeOrganization)
    .filter((item): item is BackendOrganization => item !== null);
}

export async function createOrganization(
  payload: CreateOrganizationPayload
): Promise<BackendOrganization> {
  const raw = await apiRequest<unknown>("/api/Organizations", {
    method: "POST",
    requiresAuth: true,
    body: payload,
  });

  const organization = normalizeOrganization(raw);
  if (!organization) {
    throw new Error("Invalid organization payload returned by backend.");
  }

  return organization;
}

export async function getOrganizationById(
  id: string
): Promise<BackendOrganization | null> {
  const raw = await apiRequest<unknown>(`/api/Organizations/${id}`, {
    method: "GET",
    requiresAuth: true,
  });

  return normalizeOrganization(raw);
}

export async function updateOrganization(
  id: string,
  payload: UpdateOrganizationPayload
): Promise<BackendOrganization> {
  const raw = await apiRequest<unknown>(`/api/Organizations/${id}`, {
    method: "PUT",
    requiresAuth: true,
    body: payload,
  });

  const organization = normalizeOrganization(raw);
  if (!organization) {
    throw new Error("Invalid organization payload returned by backend.");
  }

  return organization;
}

export async function deleteOrganization(id: string): Promise<void> {
  await apiRequest<void>(`/api/Organizations/${id}`, {
    method: "DELETE",
    requiresAuth: true,
  });
}

function normalizeOrganization(raw: unknown): BackendOrganization | null {
  if (!raw || typeof raw !== "object") {
    return null;
  }

  const item = raw as Record<string, unknown>;
  const id = String(item.id ?? item.Id ?? "");
  const name = String(item.name ?? item.Name ?? "");
  const slug = String(item.slug ?? item.Slug ?? "");

  if (!id || !name || !slug) {
    return null;
  }

  return {
    id,
    name,
    slug,
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
