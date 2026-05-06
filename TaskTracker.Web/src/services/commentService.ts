import { apiRequest } from "./apiClient";
import type { BackendComment } from "../types/app";

export interface CreateCommentPayload {
  taskId: number;
  content: string;
  mentionedUserIds?: string[];
}

export interface UpdateCommentPayload {
  content: string;
}

export async function getComments(taskId?: number): Promise<BackendComment[]> {
  const query = typeof taskId === "number" ? `?taskId=${taskId}` : "";
  const raw = await apiRequest<unknown[]>(`/api/Comments${query}`, {
    method: "GET",
    requiresAuth: true,
  });

  return (raw ?? [])
    .map(normalizeComment)
    .filter((item): item is BackendComment => item !== null);
}

export async function createComment(payload: CreateCommentPayload): Promise<BackendComment> {
  const raw = await apiRequest<unknown>("/api/Comments", {
    method: "POST",
    requiresAuth: true,
    body: payload,
  });

  const comment = normalizeComment(raw);
  if (!comment) {
    throw new Error("Invalid comment payload returned by backend.");
  }

  return comment;
}

export async function getCommentById(id: string): Promise<BackendComment | null> {
  const raw = await apiRequest<unknown>(`/api/Comments/${id}`, {
    method: "GET",
    requiresAuth: true,
  });

  return normalizeComment(raw);
}

export async function updateComment(
  id: string,
  payload: UpdateCommentPayload
): Promise<BackendComment> {
  const raw = await apiRequest<unknown>(`/api/Comments/${id}`, {
    method: "PUT",
    requiresAuth: true,
    body: payload,
  });

  const comment = normalizeComment(raw);
  if (!comment) {
    throw new Error("Invalid comment payload returned by backend.");
  }

  return comment;
}

export async function deleteComment(id: string): Promise<void> {
  await apiRequest<void>(`/api/Comments/${id}`, {
    method: "DELETE",
    requiresAuth: true,
  });
}

function normalizeComment(raw: unknown): BackendComment | null {
  if (!raw || typeof raw !== "object") {
    return null;
  }

  const item = raw as Record<string, unknown>;
  const id = String(item.id ?? item.Id ?? "");
  const taskIdValue = item.taskId ?? item.TaskId;
  const taskId = typeof taskIdValue === "number" ? taskIdValue : Number(taskIdValue ?? NaN);
  const authorId = String(item.authorId ?? item.AuthorId ?? "");
  const authorName = String(item.authorName ?? item.AuthorName ?? "");
  const content = String(item.content ?? item.Content ?? "");
  const createdAt = String(item.createdAt ?? item.CreatedAt ?? "");
  const updatedAt = String(item.updatedAt ?? item.UpdatedAt ?? "");

  if (!id || Number.isNaN(taskId) || !authorId || !authorName || !content || !createdAt || !updatedAt) {
    return null;
  }

  return {
    id,
    taskId,
    authorId,
    authorName,
    content,
    createdAt,
    updatedAt,
  };
}
