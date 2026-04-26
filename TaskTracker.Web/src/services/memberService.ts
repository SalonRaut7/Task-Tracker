import { apiRequest } from "./apiClient";
import type {
  ScopeMembersResponse,
  ScopeMember,
  UpdateMemberRolePayload,
  ScopeTypeNumeric,
  UserPermissions,
} from "../types/invitation";

export async function getMembersByScope(
  scopeType: ScopeTypeNumeric,
  scopeId: string
): Promise<ScopeMembersResponse> {
  return apiRequest<ScopeMembersResponse>(
    `/api/members?scopeType=${scopeType}&scopeId=${scopeId}`
  );
}

export async function updateMemberRole(
  payload: UpdateMemberRolePayload
): Promise<ScopeMember> {
  return apiRequest<ScopeMember>("/api/members/role", {
    method: "PUT",
    body: payload,
  });
}

export async function removeMember(
  scopeType: ScopeTypeNumeric,
  scopeId: string,
  userId: string
): Promise<void> {
  return apiRequest<void>(
    `/api/members?scopeType=${scopeType}&scopeId=${scopeId}&userId=${userId}`,
    { method: "DELETE" }
  );
}

export async function getMyPermissions(): Promise<UserPermissions> {
  return apiRequest<UserPermissions>("/api/me/permissions");
}
