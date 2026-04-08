import type { TaskDto, CreateTaskDto, UpdateTaskDto } from "../../types/task";
import { Status, TaskPriority } from "../../types/task";
import { priorityOptions, statusOptions } from "./taskOptions";

export type TaskDraft = Partial<TaskDto>;

export const createEmptyTaskDraft = (): TaskDraft => ({
  title: "",
  description: "",
  status: Status.NotStarted,
  priority: TaskPriority.Medium,
  startDate: null,
  endDate: null,
});

export const toDateOnlyString = (value?: string | Date | null): string | null => {
  if (!value) return null;

  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) return null;

  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
};

export const buildCreateDto = (data: TaskDraft): CreateTaskDto => ({
  title: data.title?.trim() ?? "",
  description: data.description?.trim() ?? "",
  status: data.status ?? Status.NotStarted,
  priority: data.priority ?? TaskPriority.Medium,
  startDate: toDateOnlyString(data.startDate),
  endDate: toDateOnlyString(data.endDate),
});

export const buildUpdateDto = (data: TaskDraft): UpdateTaskDto => ({
  title: data.title?.trim() ?? "",
  description: data.description?.trim() ?? "",
  status: data.status ?? Status.NotStarted,
  priority: data.priority ?? TaskPriority.Medium,
  startDate: toDateOnlyString(data.startDate),
  endDate: toDateOnlyString(data.endDate),
});

export const getErrorMessage = (err: unknown, fallback: string): string => {
  if (err instanceof Error && err.message.trim()) {
    return err.message;
  }
  return fallback;
};

export const getStatusText = (value: number | null | undefined): string =>
  statusOptions.find((x) => x.id === value)?.name ?? "Unknown";

export const getPriorityText = (value: number | null | undefined): string =>
  priorityOptions.find((x) => x.id === value)?.name ?? "Unknown";