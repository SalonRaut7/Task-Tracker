export enum Status {
  NotStarted,
  InProgress,
  Completed,
  Cancelled,
  OnHold
}

export interface TaskDto {
  id: number;
  title?: string;
  description?: string;
  status: Status;
}

export interface CreateTaskDto {
  title?: string;
  description?: string;
}

export interface UpdateTaskDto {
  title?: string;
  description?: string;
  status?: Status;
}