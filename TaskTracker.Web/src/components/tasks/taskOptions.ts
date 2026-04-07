import { Status, TaskPriority } from "../../types/task";

export const statusOptions = [
  { id: Status.NotStarted, name: "Not Started" },
  { id: Status.InProgress, name: "In Progress" },
  { id: Status.Completed, name: "Completed" },
  { id: Status.OnHold, name: "On Hold" },
  { id: Status.Cancelled, name: "Cancelled" },
];

export const priorityOptions = [
  { id: TaskPriority.Lowest, name: "Lowest" },
  { id: TaskPriority.Low, name: "Low" },
  { id: TaskPriority.Medium, name: "Medium" },
  { id: TaskPriority.High, name: "High" },
  { id: TaskPriority.Highest, name: "Highest" },
];

export const statusClassMap: Record<number, string> = {
  [Status.NotStarted]: "badge badge-gray",
  [Status.InProgress]: "badge badge-blue",
  [Status.Completed]: "badge badge-green",
  [Status.OnHold]: "badge badge-yellow",
  [Status.Cancelled]: "badge badge-red",
};

export const priorityClassMap: Record<number, string> = {
  [TaskPriority.Lowest]: "badge badge-gray",
  [TaskPriority.Low]: "badge badge-blue",
  [TaskPriority.Medium]: "badge badge-yellow",
  [TaskPriority.High]: "badge badge-orange",
  [TaskPriority.Highest]: "badge badge-red",
};