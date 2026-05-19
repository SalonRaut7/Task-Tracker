import type {
  TaskDto,
  CreateTaskDto,
  UpdateTaskDto,
  Status,
  TaskPriority,
} from "../types/task";
import { apiRequest, ACCESS_TOKEN_KEY } from "./apiClient";

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

// Export / Import

export interface ImportValidationError {
  rowNumber: number;
  field: string;
  message: string;
}

export interface ImportResult {
  createdCount: number;
  updatedCount: number;
  errors: ImportValidationError[];
}

const rawBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "";
const _apiBaseUrl = rawBaseUrl.replace(/\/+$/, "");

function resolveApiUrl(path: string): string {
  if (/^https?:\/\//i.test(path)) return path;
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return `${_apiBaseUrl}${normalizedPath}`;
}

function getAccessToken(): string | null {
  return localStorage.getItem(ACCESS_TOKEN_KEY);
}

/**
 * Downloads all tasks for the given project as an .xlsx file.
 * @param backlogOnly  When true, only exports backlog tasks (no sprint assigned).
 */
export const exportTasks = async (
  projectId: string,
  backlogOnly: boolean
): Promise<void> => {
  const params = new URLSearchParams({
    projectId,
    backlogOnly: String(backlogOnly),
  });

  const response = await fetch(resolveApiUrl(`${BASE_URL}/export?${params.toString()}`), {
    method: "GET",
    headers: {
      Authorization: `Bearer ${getAccessToken() ?? ""}`,
    },
  });

  if (!response.ok) {
    throw new Error(`Export failed (${response.status}).`);
  }

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);

  // Read filename from Content-Disposition header (e.g. "attachment; filename=PROJ.xlsx")
  let downloadName = backlogOnly ? `tasks-export.xlsx` : `TaskList.xlsx`;
  const disposition = response.headers.get("content-disposition");
  if (disposition) {
    const match = disposition.match(/filename\*?=(?:UTF-8''|")?([^";]+)/i);
    if (match?.[1]) {
      downloadName = decodeURIComponent(match[1].replace(/"/g, ""));
    }
  }

  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = downloadName;
  document.body.appendChild(anchor);
  anchor.click();
  setTimeout(() => {
    document.body.removeChild(anchor);
    URL.revokeObjectURL(url);
  }, 0);
};

/**
 * Uploads an .xlsx file and imports tasks into the given project.
 * Returns an ImportResult — check `errors.length` before treating it as success.
 */
export const importTasks = async (
  projectId: string,
  file: File
): Promise<ImportResult> => {
  const params = new URLSearchParams({ projectId });
  const formData = new FormData();
  formData.append("file", file);

  const accessToken = getAccessToken();
  if (!accessToken) {
    throw new Error("You must be signed in to import tasks.");
  }

  const response = await fetch(
    resolveApiUrl(`${BASE_URL}/import?${params.toString()}`),
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${accessToken}`,
        // Note: do NOT set Content-Type — the browser must set it with the boundary
      },
      body: formData,
    }
  );

  // 422 = validation errors returned from the backend — parse them as ImportResult
  if (response.status === 422) {
    const data = (await response.json()) as ImportResult;
    return data;
  }

  if (!response.ok) {
    let message = `Import failed (${response.status}).`;
    try {
      const err = (await response.json()) as { detail?: string; title?: string };
      message = err.detail ?? err.title ?? message;
    } catch {
      // ignore parse errors
    }
    throw new Error(message);
  }

  return (await response.json()) as ImportResult;
};
