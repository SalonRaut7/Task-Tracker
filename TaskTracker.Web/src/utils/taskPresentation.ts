import { Status, TaskPriority, type TaskDto } from "../types/task";

export const statusOptions: Array<{ id: Status; label: string }> = [
  { id: Status.NotStarted, label: "Not Started" },
  { id: Status.InProgress, label: "In Progress" },
  { id: Status.Completed, label: "Completed" },
  { id: Status.OnHold, label: "On Hold" },
  { id: Status.Cancelled, label: "Cancelled" },
];

export const priorityOptions: Array<{ id: TaskPriority; label: string }> = [
  { id: TaskPriority.Lowest, label: "Lowest" },
  { id: TaskPriority.Low, label: "Low" },
  { id: TaskPriority.Medium, label: "Medium" },
  { id: TaskPriority.High, label: "High" },
  { id: TaskPriority.Highest, label: "Highest" },
];

export function statusLabel(status: Status): string {
  return statusOptions.find((option) => option.id === status)?.label ?? "Unknown";
}

export function priorityLabel(priority: TaskPriority): string {
  return (
    priorityOptions.find((option) => option.id === priority)?.label ?? "Unknown"
  );
}

export function taskKey(task: TaskDto): string {
  return `TASK-${task.id}`;
}

export function isTaskCompleted(task: TaskDto): boolean {
  return task.status === Status.Completed;
}

export function dateOnlyToIso(dateOnly: string): string {
  return `${dateOnly}T00:00:00`;
}
