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

type RefreshResponse = {
  accessToken?: string;
  refreshToken?: string;
  user?: unknown;
};

let pendingRefreshPromise: Promise<boolean> | null = null;

function resolveUrl(path: string): string {
  if (/^https?:\/\//i.test(path)) {
    return path;
  }

  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return `${API_BASE_URL}${normalizedPath}`;
}

function tryNormalizeStoredUser(raw: unknown): Record<string, unknown> | null {
  if (!raw || typeof raw !== "object") {
    return null;
  }

  const user = raw as Record<string, unknown>;
  const id = String(user.id ?? user.Id ?? "");
  const email = String(user.email ?? user.Email ?? "");
  const firstName = String(user.firstName ?? user.FirstName ?? "");
  const lastName = String(user.lastName ?? user.LastName ?? "");

  if (!id || !email || !firstName || !lastName) {
    return null;
  }

  const rolesRaw = user.roles ?? user.Roles;
  const permissionsRaw = user.permissions ?? user.Permissions;

  const roles = Array.isArray(rolesRaw)
    ? rolesRaw.map((role) => String(role)).filter(Boolean)
    : [];
  const permissions = Array.isArray(permissionsRaw)
    ? permissionsRaw.map((permission) => String(permission)).filter(Boolean)
    : [];

  return {
    id,
    email,
    firstName,
    lastName,
    roles,
    permissions,
  };
}

async function refreshAccessToken(): Promise<boolean> {
  if (pendingRefreshPromise) {
    return pendingRefreshPromise;
  }

  const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
  if (!refreshToken) {
    return false;
  }

  pendingRefreshPromise = (async () => {
    const response = await fetch(resolveUrl("/api/Auth/refresh-token"), {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ refreshToken }),
    });

    if (!response.ok) {
      return false;
    }

    const payload = (await response.json()) as RefreshResponse;
    const accessToken =
      typeof payload.accessToken === "string" ? payload.accessToken : "";
    const nextRefreshToken =
      typeof payload.refreshToken === "string" ? payload.refreshToken : "";

    if (!accessToken || !nextRefreshToken) {
      return false;
    }

    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, nextRefreshToken);

    const normalizedUser = tryNormalizeStoredUser(payload.user);
    if (normalizedUser) {
      localStorage.setItem(USER_KEY, JSON.stringify(normalizedUser));
    }

    return true;
  })();

  try {
    return await pendingRefreshPromise;
  } finally {
    pendingRefreshPromise = null;
  }
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

  const requestBody = body === undefined ? undefined : JSON.stringify(body);

  const response = await fetch(resolveUrl(path), {
    ...rest,
    headers: resolvedHeaders,
    body: requestBody,
  });

  if (!response.ok) {
    if (response.status === 401 && requiresAuth) {
      const refreshed = await refreshAccessToken();

      if (refreshed) {
        const retryHeaders = new Headers(resolvedHeaders);
        const freshAccessToken = localStorage.getItem(ACCESS_TOKEN_KEY);

        if (freshAccessToken) {
          retryHeaders.set("Authorization", `Bearer ${freshAccessToken}`);
        }

        const retryResponse = await fetch(resolveUrl(path), {
          ...rest,
          headers: retryHeaders,
          body: requestBody,
        });

        if (retryResponse.ok) {
          if (retryResponse.status === 204) {
            return undefined as T;
          }

          return (await retryResponse.json()) as T;
        }

        if (retryResponse.status === 401) {
          window.dispatchEvent(new CustomEvent(UNAUTHORIZED_EVENT));
        }

        throw await readError(retryResponse);
      }

      window.dispatchEvent(new CustomEvent(UNAUTHORIZED_EVENT));
    }

    throw await readError(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
