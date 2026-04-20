export enum Status {
  NotStarted,
  InProgress,
  Completed,
  OnHold,
  Cancelled
}

export enum TaskPriority {
  Lowest,
  Low,
  Medium,
  High,
  Highest
}

export interface TaskDto {
  id: number;
  projectId: string;
  epicId?: string | null;
  sprintId?: string | null;
  assigneeId?: string | null;
  reporterId: string;
  title?: string;
  description?: string;
  status: Status;
  priority: TaskPriority;
  startDate: string | null;  
  endDate: string | null;    
  createdAt: string;          
  updatedAt: string;
}

export interface CreateTaskDto {
  projectId: string;
  epicId?: string | null;
  sprintId?: string | null;
  assigneeId?: string | null;
  title?: string;
  description?: string;
  status?: Status;
  priority?: TaskPriority;
  startDate?: string | null;
  endDate?: string | null;
  endDateExtensionDays?: number | null;
}

export interface UpdateTaskDto {
  epicId?: string | null;
  sprintId?: string | null;
  assigneeId?: string | null;
  title?: string;
  description?: string;
  status?: Status;
  priority?: TaskPriority;
  startDate?: string | null;
  endDate?: string | null;
  endDateExtensionDays?: number | null;
}