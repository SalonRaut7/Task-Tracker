import type {
  TaskDto,
  CreateTaskDto,
  UpdateTaskDto,
  Status,
  TaskPriority,
} from "../types/task";

export type TaskGridLoadOptions = {
  skip?: number;
  take?: number;
};

export type TaskGridLoadResult = {
  data: TaskDto[];
  totalCount: number;
};

type ProblemDetailsResponse = {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
};

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;
const BASE_URL = `${API_BASE_URL}/api/Tasks`;

let pendingTaskLoad:
  | { requestKey: string; promise: Promise<TaskGridLoadResult> }
  | null = null;

const appendBaseFilters = (
  params: URLSearchParams,
  titleContains?: string,
  status?: Status,
  priority?: TaskPriority
): void => {
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
  titleContains?: string,
  status?: Status,
  priority?: TaskPriority
): string => {
  const params = new URLSearchParams();

  appendBaseFilters(params, titleContains, status, priority);

  const skip = typeof loadOptions.skip === "number" ? loadOptions.skip : 0;
  const take = typeof loadOptions.take === "number" ? loadOptions.take : 10;

  params.append("Skip", skip.toString());
  params.append("Take", take.toString());

  return params.toString() ? `${BASE_URL}?${params.toString()}` : BASE_URL;
};

const getErrorMessage = async (res: Response): Promise<string> => {
  try {
    const contentType = res.headers.get("content-type") ?? "";

    if (contentType.includes("application/json")) {
      const data = (await res.json()) as ProblemDetailsResponse;

      if (data.errors) {
        return Object.entries(data.errors)
          .flatMap(([field, messages]) =>
            messages.map((msg) => `${field}: ${msg}`)
          )
          .join("\n");
      }

      return data.detail || data.title || `HTTP error ${res.status}`;
    }

    const text = await res.text();
    return text || `HTTP error ${res.status}`;
  } catch {
    return `HTTP error ${res.status}`;
  }
};

const request = async <T>(url: string, init?: RequestInit): Promise<T> => {
  const res = await fetch(url, init);

  if (!res.ok) {
    throw new Error(await getErrorMessage(res));
  }

  if (res.status === 204) {
    return undefined as T;
  }

  return res.json() as Promise<T>;
};

export const getTasks = async (
  titleContains?: string,
  status?: Status,
  priority?: TaskPriority
): Promise<TaskDto[]> => {
  const response = await loadTasks({}, titleContains, status, priority);
  return response.data;
};

export const loadTasks = async (
  loadOptions: TaskGridLoadOptions,
  titleContains?: string,
  status?: Status,
  priority?: TaskPriority
): Promise<TaskGridLoadResult> => {
  const requestUrl = buildLoadUrl(loadOptions, titleContains, status, priority);

  if (pendingTaskLoad?.requestKey === requestUrl) {
    return pendingTaskLoad.promise;
  }

  const currentPromise = (async () => {
    const payload = await request<Record<string, unknown>>(requestUrl);

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
  return request<TaskDto>(BASE_URL, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(task),
  });
};

export const updateTask = async (
  id: number,
  task: UpdateTaskDto
): Promise<TaskDto> => {
  return request<TaskDto>(`${BASE_URL}/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(task),
  });
};

export const deleteTask = async (id: number): Promise<void> => {
  await request<void>(`${BASE_URL}/${id}`, {
    method: "DELETE",
  });
};