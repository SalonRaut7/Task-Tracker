type ProblemDetailsResponse = {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
};

export class ApiError extends Error {
  status: number;
  details?: ProblemDetailsResponse;

  constructor(message: string, status: number, details?: ProblemDetailsResponse) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.details = details;
  }
}

export const ACCESS_TOKEN_KEY = "tasktracker-access-token";
export const REFRESH_TOKEN_KEY = "tasktracker-refresh-token";
export const USER_KEY = "tasktracker-user";
export const UNAUTHORIZED_EVENT = "tasktracker:unauthorized";

const rawBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "";
const API_BASE_URL = rawBaseUrl.replace(/\/+$/, "");

function resolveUrl(path: string): string {
  if (/^https?:\/\//i.test(path)) {
    return path;
  }

  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return `${API_BASE_URL}${normalizedPath}`;
}

async function readError(response: Response): Promise<ApiError> {
  const contentType = response.headers.get("content-type") ?? "";

  if (contentType.includes("application/json")) {
    const payload = (await response.json()) as ProblemDetailsResponse;

    const modelErrors = payload.errors
      ? Object.entries(payload.errors)
          .flatMap(([, messages]) => messages)
          .join("\n")
      : "";

    const message =
      modelErrors ||
      payload.detail ||
      payload.title ||
      `HTTP error ${response.status}`;

    return new ApiError(message, response.status, payload);
  }

  const raw = await response.text();
  return new ApiError(raw || `HTTP error ${response.status}`, response.status);
}

export interface RequestOptions extends Omit<RequestInit, "body"> {
  body?: unknown;
  requiresAuth?: boolean;
}

export async function apiRequest<T>(
  path: string,
  options: RequestOptions = {}
): Promise<T> {
  const { body, requiresAuth = true, headers, ...rest } = options;

  const resolvedHeaders = new Headers(headers);

  if (body !== undefined && !resolvedHeaders.has("Content-Type")) {
    resolvedHeaders.set("Content-Type", "application/json");
  }

  if (requiresAuth) {
    const accessToken = localStorage.getItem(ACCESS_TOKEN_KEY);
    if (accessToken) {
      resolvedHeaders.set("Authorization", `Bearer ${accessToken}`);
    }
  }

  const response = await fetch(resolveUrl(path), {
    ...rest,
    headers: resolvedHeaders,
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  if (!response.ok) {
    if (response.status === 401 && requiresAuth) {
      window.dispatchEvent(new CustomEvent(UNAUTHORIZED_EVENT));
    }

    throw await readError(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
