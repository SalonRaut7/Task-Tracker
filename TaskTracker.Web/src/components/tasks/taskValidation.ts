import type { CreateTaskDto, UpdateTaskDto } from "../../types/task";

export const isValidDateRange = (
  startDate?: string | Date | null,
  endDate?: string | Date | null
): boolean => {
  if (!startDate || !endDate) return true;

  const start = new Date(startDate);
  const end = new Date(endDate);

  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) {
    return false;
  }

  return start <= end;
};

export const validateTask = (
  task: Partial<CreateTaskDto | UpdateTaskDto>
): Record<string, string> => {
  const errors: Record<string, string> = {};

  const title = task.title?.trim() ?? "";
  const description = task.description?.trim() ?? "";

  if (!title) {
    errors.title = "Title is required.";
  } else if (title.length > 100) {
    errors.title = "Title cannot exceed 100 characters.";
  }

  if (description.length > 500) {
    errors.description = "Description cannot exceed 500 characters.";
  }

  if (task.status === undefined || task.status === null) {
    errors.status = "Status is required.";
  }

  if (task.priority === undefined || task.priority === null) {
    errors.priority = "Priority is required.";
  }

  if (!isValidDateRange(task.startDate, task.endDate)) {
    errors.endDate = "Start date must be before or equal to end date.";
  }

  return errors;
};

export const isTaskValid = (
  task: Partial<CreateTaskDto | UpdateTaskDto>
): boolean => {
  return Object.keys(validateTask(task)).length === 0;
};