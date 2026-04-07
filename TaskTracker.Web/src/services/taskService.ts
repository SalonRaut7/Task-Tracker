import type { TaskDto, CreateTaskDto, UpdateTaskDto, Status } from "../types/task";

const BASE_URL = "http://localhost:5283/api/Tasks";

const getErrorMessage = async (res: Response) => {
  try {
    const text = await res.text();
    return text || `HTTP error ${res.status}`;
  } catch {
    return `HTTP error ${res.status}`;
  }
};

export const getTasks = async (titleContains?: string, status?: Status): Promise<TaskDto[]> => {
  const params = new URLSearchParams();

  if (titleContains) params.append("TitleContains", titleContains);
  if (status !== undefined) params.append("Status", status.toString());

  const url = params.toString() ? `${BASE_URL}?${params.toString()}` : BASE_URL;
  const res = await fetch(url);

  if (!res.ok) {
    throw new Error(await getErrorMessage(res));
  }

  return res.json() as Promise<TaskDto[]>;
};

export const createTask = async (task: CreateTaskDto): Promise<TaskDto> => {
  const res = await fetch(BASE_URL, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(task),
  });

  if (!res.ok) {
    throw new Error(await getErrorMessage(res));
  }

  return res.json() as Promise<TaskDto>;
};

export const updateTask = async (id: number, task: UpdateTaskDto): Promise<TaskDto> => {
  const res = await fetch(`${BASE_URL}/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(task),
  });

  if (!res.ok) {
    throw new Error(await getErrorMessage(res));
  }

  return res.json() as Promise<TaskDto>;
};

export const deleteTask = async (id: number): Promise<void> => {
  const res = await fetch(`${BASE_URL}/${id}`, {
    method: "DELETE",
  });

  if (!res.ok) {
    throw new Error(await getErrorMessage(res));
  }
};