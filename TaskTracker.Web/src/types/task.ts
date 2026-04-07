export enum Status {
  NotStarted,
  InProgress,
  Completed,
  Cancelled,
  OnHold
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
  title?: string;
  description?: string;
  status: Status;
  priority: TaskPriority;
  startDate: string | null;  
  endDate: string | null;    
  createdAt: string;          
}

export interface CreateTaskDto {
  title?: string;
  description?: string;
  status?: Status;
  priority?: TaskPriority;
  startDate?: string | null;
  endDate?: string | null;
}

export interface UpdateTaskDto {
  title?: string;
  description?: string;
  status?: Status;
  priority?: TaskPriority;
  startDate?: string | null;
  endDate?: string | null;
}