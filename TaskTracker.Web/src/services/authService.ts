import { apiRequest } from "./apiClient";
import type {
  AuthResponse,
  ForgotPasswordPayload,
  RegisterPayload,
  RegisterResponse,
  ResetPasswordPayload,
  ResendOtpPayload,
  SimpleMessageResponse,
  VerifyEmailPayload,
  VerifyEmailResponse,
  VerifyPasswordResetOtpPayload,
  VerifyPasswordResetOtpResponse,
} from "../types/app";

export async function login(email: string, password: string): Promise<AuthResponse> {
  return apiRequest<AuthResponse>("/api/Auth/login", {
    method: "POST",
    body: { email, password },
    requiresAuth: false,
  });
}

export async function register(payload: RegisterPayload): Promise<RegisterResponse> {
  return apiRequest<RegisterResponse>("/api/Auth/register", {
    method: "POST",
    body: payload,
    requiresAuth: false,
  });
}

export async function verifyEmail(payload: VerifyEmailPayload): Promise<VerifyEmailResponse> {
  return apiRequest<VerifyEmailResponse>("/api/Auth/verify-email", {
    method: "POST",
    body: payload,
    requiresAuth: false,
  });
}

export async function resendOtp(payload: ResendOtpPayload): Promise<SimpleMessageResponse> {
  return apiRequest<SimpleMessageResponse>("/api/Auth/resend-otp", {
    method: "POST",
    body: payload,
    requiresAuth: false,
  });
}

export async function forgotPassword(
  payload: ForgotPasswordPayload
): Promise<SimpleMessageResponse> {
  return apiRequest<SimpleMessageResponse>("/api/Auth/forgot-password", {
    method: "POST",
    body: payload,
    requiresAuth: false,
  });
}

export async function resetPassword(
  payload: ResetPasswordPayload
): Promise<SimpleMessageResponse> {
  return apiRequest<SimpleMessageResponse>("/api/Auth/reset-password", {
    method: "POST",
    body: payload,
    requiresAuth: false,
  });
}

export async function verifyPasswordResetOtp(
  payload: VerifyPasswordResetOtpPayload
): Promise<VerifyPasswordResetOtpResponse> {
  return apiRequest<VerifyPasswordResetOtpResponse>("/api/Auth/verify-reset-otp", {
    method: "POST",
    body: payload,
    requiresAuth: false,
  });
}

export async function logout(refreshToken: string): Promise<SimpleMessageResponse> {
  return apiRequest<SimpleMessageResponse>("/api/Auth/logout", {
    method: "POST",
    body: { refreshToken },
    requiresAuth: true,
  });
}
