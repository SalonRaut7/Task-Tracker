import { apiRequest } from "./apiClient";
import type { AppNotification } from "../types/app";

export async function fetchNotifications(
  take = 50
): Promise<AppNotification[]> {
  return apiRequest<AppNotification[]>(`/api/Notifications?take=${take}`);
}

export async function markNotificationRead(id: string): Promise<void> {
  await apiRequest<void>(`/api/Notifications/${id}/read`, {
    method: "PATCH",
  });
}

export async function markAllNotificationsRead(): Promise<void> {
  await apiRequest<void>(`/api/Notifications/read-all`, {
    method: "PATCH",
  });
}
