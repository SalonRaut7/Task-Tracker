import { apiRequest } from "./apiClient";
import type {
  Invitation,
  CreateInvitationPayload,
  AcceptInvitationPayload,
  AcceptInvitationResult,
  ScopeTypeNumeric,
} from "../types/invitation";

export async function createInvitation(
  payload: CreateInvitationPayload
): Promise<Invitation> {
  return apiRequest<Invitation>("/api/invitations", {
    method: "POST",
    body: payload,
  });
}

export async function resendInvitation(
  invitationId: string
): Promise<Invitation> {
  return apiRequest<Invitation>(`/api/invitations/${invitationId}/resend`, {
    method: "POST",
  });
}

export async function revokeInvitation(invitationId: string): Promise<void> {
  return apiRequest<void>(`/api/invitations/${invitationId}/revoke`, {
    method: "POST",
  });
}

export async function acceptInvitation(
  payload: AcceptInvitationPayload
): Promise<AcceptInvitationResult> {
  return apiRequest<AcceptInvitationResult>("/api/invitations/accept", {
    method: "POST",
    body: payload,
  });
}

export async function getInvitationsByScope(
  scopeType: ScopeTypeNumeric,
  scopeId: string
): Promise<Invitation[]> {
  return apiRequest<Invitation[]>(
    `/api/invitations?scopeType=${scopeType}&scopeId=${scopeId}`
  );
}
