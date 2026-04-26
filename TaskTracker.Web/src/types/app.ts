export type ThemeMode = "light" | "dark";

export interface AppUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  roles: string[];
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  user: {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    roles: string[];
  };
}

export interface RegisterPayload {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface RegisterResponse {
  userId: string;
  message: string;
}

export interface VerifyEmailPayload {
  email: string;
  otpCode: string;
}

export interface VerifyEmailResponse {
  success: boolean;
  message: string;
}

export interface ResendOtpPayload {
  email: string;
}

export interface ForgotPasswordPayload {
  email: string;
}

export interface ResetPasswordPayload {
  email: string;
  otpCode: string;
  newPassword: string;
}

export interface VerifyPasswordResetOtpPayload {
  email: string;
  otpCode: string;
}

export interface VerifyPasswordResetOtpResponse {
  success: boolean;
  message: string;
}

export interface SimpleMessageResponse {
  message: string;
}

export interface BackendProject {
  id: string;
  organizationId?: string;
  name: string;
  key?: string;
  description?: string;
  status?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface BackendOrganization {
  id: string;
  name: string;
  slug: string;
  description?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface BackendEpic {
  id: string;
  projectId: string;
  title: string;
  description?: string;
  status: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface BackendSprint {
  id: string;
  projectId: string;
  name: string;
  goal?: string;
  startDate: string;
  endDate: string;
  status: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface BackendComment {
  id: string;
  taskId: number;
  authorId: string;
  authorName: string;
  content: string;
  createdAt: string;
  updatedAt: string;
}

export interface AppNotification {
  id: string;
  title: string;
  message: string;
  read: boolean;
  createdAt: string;
  type: "info" | "warning" | "success" | "error";
}
