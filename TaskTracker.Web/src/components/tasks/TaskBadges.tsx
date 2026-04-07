import { Status, TaskPriority } from "../../types/task";
import {
  statusClassMap,
  priorityClassMap,
  statusOptions,
  priorityOptions,
} from "./taskOptions";

export function StatusBadge({ value }: { value: Status | number }) {
  const className = statusClassMap[value] ?? "badge badge-gray";
  const text = statusOptions.find((x) => x.id === value)?.name ?? "Unknown";

  return <span className={className}>{text}</span>;
}

export function PriorityBadge({ value }: { value: TaskPriority | number }) {
  const className = priorityClassMap[value] ?? "badge badge-gray";
  const text = priorityOptions.find((x) => x.id === value)?.name ?? "Unknown";

  return <span className={className}>{text}</span>;
}