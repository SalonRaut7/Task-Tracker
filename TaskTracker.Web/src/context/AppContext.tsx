import {
  useCallback,
  createContext,
  useContext,
  useEffect,
  useMemo,
  useRef,
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
import {
  updateCurrentUserProfile as updateCurrentUserProfileRequest,
} from "../services/userService";
import {
  fetchNotifications as fetchNotificationsRequest,
  markNotificationRead as markNotificationReadRequest,
  markAllNotificationsRead as markAllNotificationsReadRequest,
} from "../services/notificationService";
import {
  startConnection,
  stopConnection,
  buildConnection,
} from "../services/signalRService";
import type {
  AppNotification,
  AppUser,
  BackendProject,
  CurrentUserProfile,
  RegisterPayload,
  RegisterResponse,
  ThemeMode,
  VerifyEmailResponse,
} from "../types/app";
import type { UserPermissions, ScopeType } from "../types/invitation";
import type { CreateTaskDto, TaskDto, UpdateTaskDto } from "../types/task";

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
  toastNotification: AppNotification | null;
  theme: ThemeMode;
  sidebarCollapsed: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (payload: RegisterPayload) => Promise<RegisterResponse>;
  verifyEmail: (email: string, otpCode: string) => Promise<VerifyEmailResponse>;
  resendOtp: (email: string) => Promise<string>;
  logout: () => Promise<void>;
  refreshWorkspaceData: (options?: { includeTasks?: boolean }) => Promise<void>;
  refreshPermissions: () => Promise<void>;
  updateCurrentUserProfile: (
    firstName: string,
    lastName: string
  ) => Promise<CurrentUserProfile>;
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
  dismissToast: () => void;
}

const AppContext = createContext<AppContextValue | undefined>(undefined);

const THEME_KEY = "tasktracker-theme";
const SIDEBAR_KEY = "tasktracker-sidebar";

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
  const [toastNotification, setToastNotification] =
    useState<AppNotification | null>(null);
  const [theme, setTheme] = useState<ThemeMode>("light");
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  const toastTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const signalRSetupRef = useRef(false);

  // ── Toast management ──────────────────────────────────────────
  const showToast = useCallback((notification: AppNotification) => {
    setToastNotification(notification);
    if (toastTimerRef.current) clearTimeout(toastTimerRef.current);
    toastTimerRef.current = setTimeout(() => {
      setToastNotification(null);
    }, 5000);
  }, []);

  const dismissToast = useCallback(() => {
    setToastNotification(null);
    if (toastTimerRef.current) clearTimeout(toastTimerRef.current);
  }, []);

  /**
   * Scoped permission check.
   */
  const hasPermission = useCallback(
    (
      permission: AppPermission,
      scopeType?: ScopeType,
      scopeId?: string
    ): boolean => {
      if (!userPermissions) return false;
      if (userPermissions.isSuperAdmin) return true;

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
      // Silently fail
    } finally {
      setPermissionsLoaded(true);
    }
  }, []);

  const updateCurrentUserProfile = useCallback(
    async (firstName: string, lastName: string): Promise<CurrentUserProfile> => {
      const profile = await updateCurrentUserProfileRequest({ firstName, lastName });
      const nextUser: AppUser = {
        ...(user ?? {
          id: profile.userId,
          email: profile.email,
          firstName: profile.firstName,
          lastName: profile.lastName,
          fullName: profile.fullName,
          roles: [],
        }),
        firstName: profile.firstName,
        lastName: profile.lastName,
        fullName: profile.fullName,
      };

      setUser(nextUser);
      localStorage.setItem(USER_KEY, JSON.stringify(nextUser));

      return profile;
    },
    [user]
  );

  // ── Bootstrap ──────────────────────────────────────────────────
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

  // ── Session teardown ───────────────────────────────────────────
  const clearSession = useCallback(async (callLogoutEndpoint = true) => {
    if (callLogoutEndpoint) {
      const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
      if (refreshToken) {
        try {
          await logoutRequest(refreshToken);
        } catch {
          // Ignore
        }
      }
    }

    // Stop SignalR
    await stopConnection();
    signalRSetupRef.current = false;

    setIsAuthenticated(false);
    setUser(null);
    setUserPermissions(null);
    setPermissionsLoaded(false);
    setTasks([]);
    setProjects([]);
    setNotifications([]);
    setToastNotification(null);
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
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

  // ── Data loading ───────────────────────────────────────────────
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

  // ── SignalR setup ──────────────────────────────────────────────
  useEffect(() => {
    if (!isAuthenticated || signalRSetupRef.current) return;
    signalRSetupRef.current = true;

    const setupSignalR = async () => {
      try {
        // Fetch initial notifications from REST API
        const initialNotifications = await fetchNotificationsRequest(50);
        setNotifications(initialNotifications);
      } catch {
        // Silently fail — notifications will start arriving via SignalR
      }

      // Start SignalR connection
      const conn = buildConnection();

      // Register event handlers
      conn.on("ReceiveNotification", (notification: AppNotification) => {
        setNotifications((prev) => [notification, ...prev].slice(0, 100));
        showToast(notification);
      });

      conn.on("TaskCreated", (task: TaskDto) => {
        setTasks((prev) => [task, ...prev]);
      });

      conn.on("TaskUpdated", (task: TaskDto) => {
        setTasks((prev) =>
          prev.map((t) => (t.id === task.id ? task : t))
        );
      });

      conn.on("TaskDeleted", (taskId: number) => {
        setTasks((prev) => prev.filter((t) => t.id !== taskId));
      });

      conn.on("ScopeMembersChanged", () => {
        void refreshPermissions();
        void refreshWorkspaceData({ includeTasks: false });
      });

      conn.on("UserWorkspaceChanged", () => {
        void refreshPermissions();
        void refreshWorkspaceData({ includeTasks: false });
      });

      conn.onreconnected(() => {
        console.log("[SignalR] Reconnected — refreshing data");
        void refreshWorkspaceData();
      });

      await startConnection();
    };

    void setupSignalR();

    return () => {
      // Cleanup on unmount (not on re-render)
    };
  }, [isAuthenticated, refreshPermissions, refreshWorkspaceData, showToast]);

  // Fetch permissions and workspace data when authenticated
  useEffect(() => {
    if (isAuthenticated) {
      void refreshPermissions().catch(() => undefined);
      void refreshWorkspaceData().catch(() => undefined);
    }
  }, [isAuthenticated, refreshPermissions, refreshWorkspaceData]);

  // ── Auth actions ───────────────────────────────────────────────
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
    signalRSetupRef.current = false; // Allow re-setup
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

  // ── Task CRUD ──────────────────────────────────────────────────
  const addTask = async (taskInput: CreateTaskDto): Promise<TaskDto> => {
    const created = await createTaskRequest(taskInput);
    setTasks((prev) => [created, ...prev]);
    return created;
  };

  const updateTask = async (
    taskId: number,
    projectId: string,
    updates: UpdateTaskDto
  ): Promise<TaskDto> => {
    const updated = await updateTaskRequest(taskId, projectId, updates);
    setTasks((prev) =>
      prev.map((task) => (task.id === taskId ? updated : task))
    );
    return updated;
  };

  const deleteTask = async (
    taskId: number,
    projectId: string
  ): Promise<void> => {
    await deleteTaskRequest(taskId, projectId);
    setTasks((prev) => prev.filter((task) => task.id !== taskId));
  };

  // ── Notification actions ───────────────────────────────────────
  const markNotificationRead = (id: string) => {
    setNotifications((prev) =>
      prev.map((item) =>
        item.id === id ? { ...item, isRead: true } : item
      )
    );
    // Fire-and-forget REST call
    void markNotificationReadRequest(id).catch(() => undefined);
  };

  const markAllNotificationsRead = () => {
    setNotifications((prev) =>
      prev.map((item) => ({ ...item, isRead: true }))
    );
    void markAllNotificationsReadRequest().catch(() => undefined);
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
      toastNotification,
      theme,
      sidebarCollapsed,
      login,
      register,
      verifyEmail,
      resendOtp,
      logout,
      refreshWorkspaceData,
      refreshPermissions,
      updateCurrentUserProfile,
      toggleTheme,
      toggleSidebar,
      addTask,
      updateTask,
      deleteTask,
      markNotificationRead,
      markAllNotificationsRead,
      dismissToast,
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
      toastNotification,
      theme,
      sidebarCollapsed,
      refreshWorkspaceData,
      refreshPermissions,
      updateCurrentUserProfile,
      dismissToast,
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
