import type { CreateTaskDto, TaskDto, UpdateTaskDto } from "../../types/task";
import { addDateOnlyDays, toDateOnlyString } from "./taskHelpers";

const allowedEndDateExtensionDays = [1, 5, 10];

type ValidationContext = {
  mode?: "add" | "edit";
  originalTask?: TaskDto | null;
};

const getTodayDateOnlyString = (): string => {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return toDateOnlyString(today) ?? "";
};

const isBeforeToday = (value?: string | Date | null): boolean => {
  const normalized = toDateOnlyString(value);
  if (!normalized) return false;

  return normalized < getTodayDateOnlyString();
};

const isDifferentDate = (
  left?: string | Date | null,
  right?: string | Date | null
): boolean => {
  return toDateOnlyString(left) !== toDateOnlyString(right);
};

export const isValidDateRange = (
  startDate?: string | Date | null,
  endDate?: string | Date | null
): boolean => {
  if (!startDate || !endDate) return true;

  const start = new Date(toDateOnlyString(startDate) ?? "");
  const end = new Date(toDateOnlyString(endDate) ?? "");

  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) {
    return false;
  }

  return start <= end;
};

export const validateTask = (
  task: Partial<CreateTaskDto | UpdateTaskDto>,
  context: ValidationContext = {}
): Record<string, string> => {
  const errors: Record<string, string> = {};
  const isEdit = context.mode === "edit";
  const originalTask = context.originalTask ?? null;

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

  if (
    task.endDateExtensionDays != null &&
    !allowedEndDateExtensionDays.includes(task.endDateExtensionDays)
  ) {
    errors.endDateExtensionDays = "End date extension must be +1, +5, or +10 days.";
  }

  const originalStartDate = originalTask?.startDate ?? null;
  const originalEndDate = originalTask?.endDate ?? null;
  const hasExtension = task.endDateExtensionDays != null;
  const effectiveEndDate = hasExtension
    ? addDateOnlyDays(originalEndDate, task.endDateExtensionDays ?? 0)
    : toDateOnlyString(task.endDate);

  if (isEdit && originalTask) {
    if (
      task.startDate != null &&
      isDifferentDate(task.startDate, originalStartDate) &&
      isBeforeToday(task.startDate)
    ) {
      errors.startDate = "Start date cannot be changed to a past date.";
    }

    if (hasExtension) {
      if (!originalEndDate) {
        errors.endDateExtensionDays = "End date extension is only available when an end date exists.";
      } else if (task.startDate && effectiveEndDate && !isValidDateRange(task.startDate, effectiveEndDate)) {
        errors.startDate = "Start date must be before or equal to the extended end date.";
      }
    } else if (task.endDate != null) {
      const normalizedEndDate = toDateOnlyString(task.endDate);

      if (
        isDifferentDate(task.endDate, originalEndDate) &&
        isBeforeToday(normalizedEndDate)
      ) {
        const canExtendPastEndDate =
          !!originalEndDate &&
          !!normalizedEndDate &&
          originalEndDate < getTodayDateOnlyString() &&
          normalizedEndDate > originalEndDate;

        if (!canExtendPastEndDate) {
          errors.endDate = "End date cannot be changed to a past date unless it is being extended.";
        }
      }
    }
  }

  if (!errors.startDate && !errors.endDate && !errors.endDateExtensionDays && !hasExtension) {
    if (!isValidDateRange(task.startDate, task.endDate)) {
      errors.endDate = "Start date must be before or equal to end date.";
    }
  }

  if (hasExtension && originalEndDate && task.startDate && effectiveEndDate) {
    if (!isValidDateRange(task.startDate, effectiveEndDate)) {
      errors.startDate = "Start date must be before or equal to the extended end date.";
    }
  }

  if (task.endDateExtensionDays != null && !isEdit) {
    errors.endDateExtensionDays = "End date extensions are only available while editing a task.";
  }

  if (!hasExtension && !isValidDateRange(task.startDate, task.endDate)) {
    errors.endDate = "Start date must be before or equal to end date.";
  }

  return errors;
};

export const isTaskValid = (
  task: Partial<CreateTaskDto | UpdateTaskDto>,
  context: ValidationContext = {}
): boolean => {
  return Object.keys(validateTask(task, context)).length === 0;
};