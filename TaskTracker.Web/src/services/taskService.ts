import type {
  TaskDto,
  CreateTaskDto,
  UpdateTaskDto,
  Status,
  TaskPriority,
} from "../types/task";
import { apiRequest } from "./apiClient";

export type TaskGridLoadOptions = {
  skip?: number;
  take?: number;
};

export type TaskGridLoadResult = {
  data: TaskDto[];
  totalCount: number;
};

const BASE_URL = "/api/Tasks";

let pendingTaskLoad:
  | { requestKey: string; promise: Promise<TaskGridLoadResult> }
  | null = null;

const appendBaseFilters = (
  params: URLSearchParams,
  projectId?: string,
  epicId?: string,
  sprintId?: string,
  assigneeId?: string,
  reporterId?: string,
  titleContains?: string,
  status?: Status,
  priority?: TaskPriority
): void => {
  if (projectId) {
    params.append("ProjectId", projectId);
  }

  if (epicId) {
    params.append("EpicId", epicId);
  }

  if (sprintId) {
    params.append("SprintId", sprintId);
  }

  if (assigneeId) {
    params.append("AssigneeId", assigneeId);
  }

  if (reporterId) {
    params.append("ReporterId", reporterId);
  }

  if (titleContains) {
    params.append("TitleContains", titleContains);
  }

  if (status !== undefined) {
    params.append("Status", status.toString());
  }

  if (priority !== undefined) {
    params.append("Priority", priority.toString());
  }
};

const buildLoadUrl = (
  loadOptions: TaskGridLoadOptions,
  projectId?: string,
  epicId?: string,
  sprintId?: string,
  assigneeId?: string,
  reporterId?: string,
  titleContains?: string,
  status?: Status,
  priority?: TaskPriority
): string => {
  const params = new URLSearchParams();

  appendBaseFilters(
    params,
    projectId,
    epicId,
    sprintId,
    assigneeId,
    reporterId,
    titleContains,
    status,
    priority
  );

  const skip = typeof loadOptions.skip === "number" ? loadOptions.skip : 0;
  const take = typeof loadOptions.take === "number" ? loadOptions.take : 10;

  params.append("Skip", skip.toString());
  params.append("Take", take.toString());

  return params.toString() ? `${BASE_URL}?${params.toString()}` : BASE_URL;
};

export const getTasks = async (
  projectId?: string,
  epicId?: string,
  sprintId?: string,
  assigneeId?: string,
  reporterId?: string,
  titleContains?: string,
  status?: Status,
  priority?: TaskPriority
): Promise<TaskDto[]> => {
  const response = await loadTasks(
    {},
    projectId,
    epicId,
    sprintId,
    assigneeId,
    reporterId,
    titleContains,
    status,
    priority
  );
  return response.data;
};

export const loadTasks = async (
  loadOptions: TaskGridLoadOptions,
  projectId?: string,
  epicId?: string,
  sprintId?: string,
  assigneeId?: string,
  reporterId?: string,
  titleContains?: string,
  status?: Status,
  priority?: TaskPriority
): Promise<TaskGridLoadResult> => {
  const requestUrl = buildLoadUrl(
    loadOptions,
    projectId,
    epicId,
    sprintId,
    assigneeId,
    reporterId,
    titleContains,
    status,
    priority
  );

  if (pendingTaskLoad?.requestKey === requestUrl) {
    return pendingTaskLoad.promise;
  }

  const currentPromise = (async () => {
    const payload = await apiRequest<Record<string, unknown>>(requestUrl, {
      method: "GET",
      requiresAuth: true,
    });

    const data =
      (Array.isArray(payload.data)
        ? payload.data
        : Array.isArray(payload.Data)
        ? payload.Data
        : []) as TaskDto[];

    const rawTotalCount = payload.totalCount ?? payload.TotalCount;
    const totalCount =
      typeof rawTotalCount === "number"
        ? rawTotalCount
        : Number.parseInt(String(rawTotalCount ?? 0), 10) || 0;

    return {
      data,
      totalCount,
    };
  })();

  pendingTaskLoad = {
    requestKey: requestUrl,
    promise: currentPromise,
  };

  try {
    return await currentPromise;
  } finally {
    if (pendingTaskLoad?.requestKey === requestUrl) {
      pendingTaskLoad = null;
    }
  }
};

export const createTask = async (task: CreateTaskDto): Promise<TaskDto> => {
  return apiRequest<TaskDto>(BASE_URL, {
    method: "POST",
    body: task,
    requiresAuth: true,
  });
};

export const getTaskById = async (
  id: number,
  projectId: string
): Promise<TaskDto | null> => {
  const query = new URLSearchParams({ projectId });
  return apiRequest<TaskDto | null>(`${BASE_URL}/${id}?${query.toString()}`, {
    method: "GET",
    requiresAuth: true,
  });
};

export const updateTask = async (
  id: number,
  projectId: string,
  task: UpdateTaskDto
): Promise<TaskDto> => {
  const query = new URLSearchParams({ projectId });
  return apiRequest<TaskDto>(`${BASE_URL}/${id}?${query.toString()}`, {
    method: "PUT",
    body: task,
    requiresAuth: true,
  });
};

export const deleteTask = async (id: number, projectId: string): Promise<void> => {
  const query = new URLSearchParams({ projectId });
  await apiRequest<void>(`${BASE_URL}/${id}?${query.toString()}`, {
    method: "DELETE",
    requiresAuth: true,
  });
};