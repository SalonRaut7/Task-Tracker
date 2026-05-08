import type { TaskAttachmentDto } from "../types/task";
import { ACCESS_TOKEN_KEY } from "./apiClient";
import { apiRequest } from "./apiClient";

const rawBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "";
const API_BASE_URL = rawBaseUrl.replace(/\/+$/, "");
const BASE_URL = "/api/TaskAttachments";

function resolveUrl(path: string): string {
  if (/^https?:\/\//i.test(path)) return path;
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return `${API_BASE_URL}${normalizedPath}`;
}

export function getAttachmentDownloadUrl(taskId: number, attachmentId: string): string {
  const query = new URLSearchParams({ taskId: taskId.toString() });
  return resolveUrl(`${BASE_URL}/${attachmentId}/download?${query.toString()}`);
}

/**
 * Upload a single file attachment to a task.
 * Uses raw fetch (not apiRequest) to send multipart/form-data.
 */
export const uploadAttachment = async (
  taskId: number,
  file: File
): Promise<TaskAttachmentDto> => {
  const formData = new FormData();
  formData.append("taskId", taskId.toString());
  formData.append("file", file);

  const accessToken = localStorage.getItem(ACCESS_TOKEN_KEY);

  const response = await fetch(resolveUrl(BASE_URL), {
    method: "POST",
    headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : {},
    body: formData,
  });

  if (!response.ok) {
    const contentType = response.headers.get("content-type") ?? "";
    if (contentType.includes("json")) {
      const payload = await response.json();
      const detail =
        payload.detail || payload.title || `Upload failed (${response.status})`;
      throw new Error(detail);
    }
    throw new Error(`Upload failed: HTTP ${response.status}`);
  }

  return (await response.json()) as TaskAttachmentDto;
};

/**
 * List all attachments for a task.
 */
export const getAttachments = async (
  taskId: number
): Promise<TaskAttachmentDto[]> => {
  const query = new URLSearchParams({ taskId: taskId.toString() });
  return apiRequest<TaskAttachmentDto[]>(`${BASE_URL}?${query.toString()}`, {
    method: "GET",
    requiresAuth: true,
  });
};

/**
 * Delete an attachment by ID.
 */
export const deleteAttachment = async (
  id: string,
  taskId: number
): Promise<void> => {
  const query = new URLSearchParams({ taskId: taskId.toString() });
  await apiRequest<void>(`${BASE_URL}/${id}?${query.toString()}`, {
    method: "DELETE",
    requiresAuth: true,
  });
};

export const downloadAttachment = async (
  taskId: number,
  attachmentId: string,
  fileName: string
): Promise<void> => {
  const accessToken = localStorage.getItem(ACCESS_TOKEN_KEY);
  const response = await fetch(getAttachmentDownloadUrl(taskId, attachmentId), {
    headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : {},
  });

  if (!response.ok) {
    throw new Error(`Download failed: HTTP ${response.status}`);
  }

  const blob = await response.blob();
  const objectUrl = URL.createObjectURL(blob);

  try {
    const link = document.createElement("a");
    link.href = objectUrl;
    link.download = fileName;
    link.rel = "noopener noreferrer";
    document.body.appendChild(link);
    link.click();
    link.remove();
  } finally {
    window.setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
  }
};
