import type {
  TaskDto,
  CreateTaskDto,
  UpdateTaskDto,
  Status,
  TaskPriority,
} from "../types/task";

type ProblemDetailsResponse = {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
};

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;
const BASE_URL = `${API_BASE_URL}/api/Tasks`;

const buildUrl = (
  titleContains?: string,
  status?: Status,
  priority?: TaskPriority
): string => {
  const params = new URLSearchParams();

  if (titleContains) params.append("TitleContains", titleContains);
  if (status !== undefined) params.append("Status", status.toString());
  if (priority !== undefined) params.append("Priority", priority.toString());

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
  return request<TaskDto[]>(buildUrl(titleContains, status, priority));
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