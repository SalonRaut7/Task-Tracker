import {
  useCallback,
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import {
  ACCESS_TOKEN_KEY,
  REFRESH_TOKEN_KEY,
  USER_KEY,
  ApiError,
  UNAUTHORIZED_EVENT,
} from "../services/apiClient";
import type { AppPermission } from "../security/permissions";
import {
  login as loginRequest,
  logout as logoutRequest,
  register as registerRequest,
  resendOtp as resendOtpRequest,
  verifyEmail as verifyEmailRequest,
} from "../services/authService";
import {
  createTask as createTaskRequest,
  deleteTask as deleteTaskRequest,
  loadTasks,
  updateTask as updateTaskRequest,
} from "../services/taskService";
import { getProjects } from "../services/projectService";
import { getMyPermissions } from "../services/memberService";
import type {
  AppNotification,
  AppUser,
  BackendProject,
  RegisterPayload,
  RegisterResponse,
  ThemeMode,
  VerifyEmailResponse,
} from "../types/app";
import type { UserPermissions, ScopeType } from "../types/invitation";
import type { CreateTaskDto, TaskDto, UpdateTaskDto } from "../types/task";
import { dateOnlyToIso, isTaskCompleted } from "../utils/taskPresentation";

interface AppContextValue {
  user: AppUser | null;
  userPermissions: UserPermissions | null;
  permissionsLoaded: boolean;
  hasPermission: (
    permission: AppPermission,
    scopeType?: ScopeType,
    scopeId?: string
  ) => boolean;
  isAuthenticated: boolean;
  bootstrapping: boolean;
  loadingData: boolean;
  tasks: TaskDto[];
  projects: BackendProject[];
  projectsApiAvailable: boolean;
  projectsApiMessage: string;
  notifications: AppNotification[];
  theme: ThemeMode;
  sidebarCollapsed: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (payload: RegisterPayload) => Promise<RegisterResponse>;
  verifyEmail: (email: string, otpCode: string) => Promise<VerifyEmailResponse>;
  resendOtp: (email: string) => Promise<string>;
  logout: () => Promise<void>;
  refreshWorkspaceData: (options?: { includeTasks?: boolean }) => Promise<void>;
  refreshPermissions: () => Promise<void>;
  toggleTheme: () => void;
  toggleSidebar: () => void;
  addTask: (task: CreateTaskDto) => Promise<TaskDto>;
  updateTask: (
    taskId: number,
    projectId: string,
    updates: UpdateTaskDto
  ) => Promise<TaskDto>;
  deleteTask: (taskId: number, projectId: string) => Promise<void>;
  markNotificationRead: (id: string) => void;
  markAllNotificationsRead: () => void;
}

const AppContext = createContext<AppContextValue | undefined>(undefined);

const THEME_KEY = "tasktracker-theme";
const SIDEBAR_KEY = "tasktracker-sidebar";
const READ_NOTIFICATION_IDS_KEY = "tasktracker-read-notifications";

function toAppUser(raw: unknown): AppUser | null {
  if (!raw || typeof raw !== "object") {
    return null;
  }

  const user = raw as Record<string, unknown>;
  const id = String(user.id ?? "");
  const email = String(user.email ?? "");
  const firstName = String(user.firstName ?? "");
  const lastName = String(user.lastName ?? "");
  const roles = Array.isArray(user.roles)
    ? user.roles.map((role) => String(role)).filter(Boolean)
    : [];

  if (!id || !email || !firstName || !lastName) {
    return null;
  }

  return {
    id,
    email,
    firstName,
    lastName,
    fullName: `${firstName} ${lastName}`.trim(),
    roles,
  };
}

function buildNotifications(tasks: TaskDto[]): AppNotification[] {
  const now = new Date();
  const dayMs = 24 * 60 * 60 * 1000;

  const derived = tasks
    .flatMap((task): AppNotification[] => {
      const entries: AppNotification[] = [];

      if (task.endDate) {
        const dueDate = new Date(dateOnlyToIso(task.endDate));
        const diff = dueDate.getTime() - now.getTime();

        if (!isTaskCompleted(task) && diff < 0) {
          entries.push({
            id: `overdue-${task.id}`,
            title: "Task overdue",
            message: `Task ${task.title || `#${task.id}`} is past its end date.`,
            createdAt: task.updatedAt,
            type: "warning",
            read: false,
          });
        }

        if (!isTaskCompleted(task) && diff >= 0 && diff <= dayMs) {
          entries.push({
            id: `due-soon-${task.id}`,
            title: "Task due soon",
            message: `Task ${task.title || `#${task.id}`} is due within 24 hours.`,
            createdAt: task.updatedAt,
            type: "info",
            read: false,
          });
        }
      }

      if (isTaskCompleted(task)) {
        entries.push({
          id: `completed-${task.id}`,
          title: "Task completed",
          message: `Task ${task.title || `#${task.id}`} is marked as completed.`,
          createdAt: task.updatedAt,
          type: "success",
          read: false,
        });
      }

      return entries;
    })
    .sort(
      (a, b) =>
        new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    )
    .slice(0, 10);

  const rawReadIds = localStorage.getItem(READ_NOTIFICATION_IDS_KEY);
  const readIds = new Set(
    rawReadIds ? (JSON.parse(rawReadIds) as string[]) : []
  );

  return derived.map((item) => ({
    ...item,
    read: readIds.has(item.id),
  }));
}

function saveReadNotificationIds(notifications: AppNotification[]): void {
  const readIds = notifications
    .filter((item) => item.read)
    .map((item) => item.id);

  localStorage.setItem(READ_NOTIFICATION_IDS_KEY, JSON.stringify(readIds));
}

export function AppProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AppUser | null>(null);
  const [userPermissions, setUserPermissions] =
    useState<UserPermissions | null>(null);
  const [permissionsLoaded, setPermissionsLoaded] = useState(false);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [bootstrapping, setBootstrapping] = useState(true);
  const [loadingData, setLoadingData] = useState(false);
  const [tasks, setTasks] = useState<TaskDto[]>([]);
  const [projects, setProjects] = useState<BackendProject[]>([]);
  const [projectsApiAvailable, setProjectsApiAvailable] = useState(true);
  const [projectsApiMessage, setProjectsApiMessage] = useState("");
  const [notifications, setNotifications] = useState<AppNotification[]>([]);
  const [theme, setTheme] = useState<ThemeMode>("light");
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  /**
   * Scoped permission check.
   * SuperAdmin → always true.
   * If scopeType/scopeId provided → checks that specific scope.
   * If no scope → checks if permission exists in ANY scope.
   */
  const hasPermission = useCallback(
    (
      permission: AppPermission,
      scopeType?: ScopeType,
      scopeId?: string
    ): boolean => {
      if (!userPermissions) return false;
      if (userPermissions.isSuperAdmin) return true;

      // Check specific scope
      if (scopeType && scopeId) {
        if (scopeType === "Organization") {
          const orgRole = userPermissions.organizationRoles.find(
            (r) => r.organizationId === scopeId
          );
          return orgRole?.permissions.includes(permission) ?? false;
        }
        if (scopeType === "Project") {
          const projRole = userPermissions.projectRoles.find(
            (r) => r.projectId === scopeId
          );
          if (projRole?.permissions.includes(permission)) return true;

          // Fall back to org role. If no direct project membership exists,
          // resolve organization from loaded projects.
          const organizationId =
            projRole?.organizationId ??
            projects.find((project) => project.id === scopeId)?.organizationId;

          if (!organizationId) {
            return false;
          }

          const orgRole = userPermissions.organizationRoles.find(
            (r) => r.organizationId === organizationId
          );
          return orgRole?.permissions.includes(permission) ?? false;
        }
      }

      // No scope specified — check if permission exists in ANY scope
      const inAnyOrg = userPermissions.organizationRoles.some((r) =>
        r.permissions.includes(permission)
      );
      if (inAnyOrg) return true;

      const inAnyProject = userPermissions.projectRoles.some((r) =>
        r.permissions.includes(permission)
      );
      return inAnyProject;
    },
    [projects, userPermissions]
  );

  const refreshPermissions = useCallback(async () => {
    if (!localStorage.getItem(ACCESS_TOKEN_KEY)) {
      setPermissionsLoaded(false);
      return;
    }

    try {
      const perms = await getMyPermissions();
      setUserPermissions(perms);
    } catch {
      // Silently fail — permissions will be stale until next refresh
    } finally {
      setPermissionsLoaded(true);
    }
  }, []);

  useEffect(() => {
    const savedTheme = localStorage.getItem(THEME_KEY) as ThemeMode | null;
    const resolvedTheme =
      savedTheme ??
      (window.matchMedia("(prefers-color-scheme: dark)").matches
        ? "dark"
        : "light");

    setTheme(resolvedTheme);
    document.documentElement.dataset.theme = resolvedTheme;

    const accessToken = localStorage.getItem(ACCESS_TOKEN_KEY);
    let parsedUser: AppUser | null = null;

    try {
      parsedUser = toAppUser(
        localStorage.getItem(USER_KEY)
          ? JSON.parse(localStorage.getItem(USER_KEY) as string)
          : null
      );
    } catch {
      parsedUser = null;
    }

    if (accessToken && parsedUser) {
      setIsAuthenticated(true);
      setUser(parsedUser);
      setPermissionsLoaded(false);
    }

    setSidebarCollapsed(localStorage.getItem(SIDEBAR_KEY) === "collapsed");
    setBootstrapping(false);
  }, []);

  const clearSession = useCallback(async (callLogoutEndpoint = true) => {
    if (callLogoutEndpoint) {
      const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
      if (refreshToken) {
        try {
          await logoutRequest(refreshToken);
        } catch {
          // Ignore API logout failures and clear local session anyway.
        }
      }
    }

    setIsAuthenticated(false);
    setUser(null);
    setUserPermissions(null);
    setPermissionsLoaded(false);
    setTasks([]);
    setProjects([]);
    setNotifications([]);
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(READ_NOTIFICATION_IDS_KEY);
  }, []);

  useEffect(() => {
    const onUnauthorized = () => {
      void clearSession(false);
    };

    window.addEventListener(UNAUTHORIZED_EVENT, onUnauthorized);
    return () => {
      window.removeEventListener(UNAUTHORIZED_EVENT, onUnauthorized);
    };
  }, [clearSession]);

  const refreshWorkspaceData = useCallback(
    async (options?: { includeTasks?: boolean }) => {
      if (!localStorage.getItem(ACCESS_TOKEN_KEY)) {
        return;
      }

      const includeTasks = options?.includeTasks ?? true;

      setLoadingData(true);

      try {
        const [taskResponse, projectResponse] = await Promise.all([
          includeTasks
            ? loadTasks({ skip: 0, take: 500 })
            : Promise.resolve(null),
          getProjects(),
        ]);

        if (taskResponse) {
          setTasks(taskResponse.data);
          setNotifications(buildNotifications(taskResponse.data));
        }

        setProjects(projectResponse.items);
        setProjectsApiAvailable(projectResponse.available);
        setProjectsApiMessage(projectResponse.message ?? "");
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          await clearSession(false);
          return;
        }

        if (error instanceof ApiError && error.status === 403) {
          setTasks([]);
          setNotifications([]);
          setProjects([]);
          setProjectsApiAvailable(false);
          setProjectsApiMessage(
            "You do not have permission to load this workspace."
          );
          return;
        }

        throw error;
      } finally {
        setLoadingData(false);
      }
    },
    [clearSession]
  );

  // Fetch permissions and workspace data when authenticated
  useEffect(() => {
    if (isAuthenticated) {
      void refreshPermissions().catch(() => undefined);
      void refreshWorkspaceData().catch(() => undefined);
    }
  }, [isAuthenticated, refreshPermissions, refreshWorkspaceData]);

  const login = async (email: string, password: string): Promise<void> => {
    const auth = await loginRequest(email.trim(), password);

    const nextUser = toAppUser(auth.user);
    if (!nextUser) {
      throw new Error("Invalid user payload received from backend.");
    }

    localStorage.setItem(ACCESS_TOKEN_KEY, auth.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, auth.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(nextUser));

    setUser(nextUser);
    setPermissionsLoaded(false);
    setIsAuthenticated(true);
  };

  const register = async (
    payload: RegisterPayload
  ): Promise<RegisterResponse> => {
    return registerRequest(payload);
  };

  const verifyEmail = async (
    email: string,
    otpCode: string
  ): Promise<VerifyEmailResponse> => {
    return verifyEmailRequest({
      email: email.trim(),
      otpCode: otpCode.trim(),
    });
  };

  const resendOtp = async (email: string): Promise<string> => {
    const response = await resendOtpRequest({ email: email.trim() });
    return response.message;
  };

  const logout = async () => {
    await clearSession(true);
  };

  const toggleTheme = () => {
    setTheme((prev) => {
      const next: ThemeMode = prev === "light" ? "dark" : "light";
      document.documentElement.dataset.theme = next;
      localStorage.setItem(THEME_KEY, next);
      return next;
    });
  };

  const toggleSidebar = () => {
    setSidebarCollapsed((prev) => {
      const next = !prev;
      localStorage.setItem(SIDEBAR_KEY, next ? "collapsed" : "expanded");
      return next;
    });
  };

  const addTask = async (taskInput: CreateTaskDto): Promise<TaskDto> => {
    const created = await createTaskRequest(taskInput);
    setTasks((prev) => {
      const next = [created, ...prev];
      setNotifications(buildNotifications(next));
      return next;
    });
    return created;
  };

  const updateTask = async (
    taskId: number,
    projectId: string,
    updates: UpdateTaskDto
  ): Promise<TaskDto> => {
    const updated = await updateTaskRequest(taskId, projectId, updates);
    setTasks((prev) => {
      const next = prev.map((task) => (task.id === taskId ? updated : task));
      setNotifications(buildNotifications(next));
      return next;
    });
    return updated;
  };

  const deleteTask = async (
    taskId: number,
    projectId: string
  ): Promise<void> => {
    await deleteTaskRequest(taskId, projectId);
    setTasks((prev) => {
      const next = prev.filter((task) => task.id !== taskId);
      setNotifications(buildNotifications(next));
      return next;
    });
  };

  const markNotificationRead = (id: string) => {
    setNotifications((prev) => {
      const next = prev.map((item) =>
        item.id === id ? { ...item, read: true } : item
      );
      saveReadNotificationIds(next);
      return next;
    });
  };

  const markAllNotificationsRead = () => {
    setNotifications((prev) => {
      const next = prev.map((item) => ({ ...item, read: true }));
      saveReadNotificationIds(next);
      return next;
    });
  };

  const value = useMemo<AppContextValue>(
    () => ({
      user,
      userPermissions,
      permissionsLoaded,
      hasPermission,
      isAuthenticated,
      bootstrapping,
      loadingData,
      tasks,
      projects,
      projectsApiAvailable,
      projectsApiMessage,
      notifications,
      theme,
      sidebarCollapsed,
      login,
      register,
      verifyEmail,
      resendOtp,
      logout,
      refreshWorkspaceData,
      refreshPermissions,
      toggleTheme,
      toggleSidebar,
      addTask,
      updateTask,
      deleteTask,
      markNotificationRead,
      markAllNotificationsRead,
    }),
    [
      user,
      userPermissions,
      permissionsLoaded,
      hasPermission,
      isAuthenticated,
      bootstrapping,
      loadingData,
      tasks,
      projects,
      projectsApiAvailable,
      projectsApiMessage,
      notifications,
      theme,
      sidebarCollapsed,
      refreshWorkspaceData,
      refreshPermissions,
    ]
  );

  return <AppContext.Provider value={value}>{children}</AppContext.Provider>;
}

export function useApp() {
  const context = useContext(AppContext);

  if (!context) {
    throw new Error("useApp must be used inside AppProvider");
  }

  return context;
}
