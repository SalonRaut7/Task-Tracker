import { useState, useEffect, useCallback } from "react";
import { Link, Navigate, useParams } from "react-router-dom";
import { useApp } from "../context/AppContext";
import { getOrganizationById } from "../services/organizationService";
import { getProjectById } from "../services/projectService";
import { getMembersByScope } from "../services/memberService";
import {
  createInvitation,
  resendInvitation,
  revokeInvitation,
} from "../services/invitationService";
import {
  updateMemberRole,
  removeMember,
} from "../services/memberService";
import {
  organizationRoleOptions,
  projectRoleOptions,
  AppPermissions,
  AppRoles,
} from "../security/permissions";
import type {
  ScopeType,
  ScopeTypeNumeric,
  ScopeMembersResponse,
} from "../types/invitation";
import "../styles/members.css";
import { getErrorMessage } from "../utils/getErrorMessage";

type ScopeInfo = {
  scopeType: ScopeTypeNumeric;
  scopeId: string;
  scopeLabel: string;
};

function useScopeInfo(): ScopeInfo {
  const { orgId, projectId } = useParams<{
    orgId?: string;
    projectId?: string;
  }>();

  if (projectId) {
    return {
      scopeType: 1,
      scopeId: projectId,
      scopeLabel: "Project",
    };
  }
  return {
    scopeType: 0,
    scopeId: orgId ?? "",
    scopeLabel: "Organization",
  };
}

export default function MembersPage() {
  const { user, userPermissions, hasPermission, refreshPermissions } = useApp();
  const { scopeType, scopeId, scopeLabel } = useScopeInfo();
  const scopeTypeName: ScopeType =
    scopeType === 0 ? "Organization" : "Project";
  const scopeHomeHref = scopeType === 0 ? "/organizations" : "/projects";
  const backLabel = scopeType === 0 ? "Organizations" : "Projects";
  const [scopeDisplayName, setScopeDisplayName] = useState(scopeLabel);

  const currentOrganizationRole =
    scopeType === 0
      ? userPermissions?.organizationRoles.find(
          (r) => r.organizationId === scopeId
        )?.role ?? null
      : null;

  const currentProjectRole =
    scopeType === 1
      ? userPermissions?.projectRoles.find((r) => r.projectId === scopeId)
          ?.role ?? null
      : null;

  const scopeName = scopeDisplayName || scopeLabel;

  const canInviteInScope =
    scopeType === 0
      ? Boolean(
          userPermissions?.isSuperAdmin ||
            currentOrganizationRole === AppRoles.OrgAdmin
        )
      : true;

  const canRevokeInScope =
    scopeType === 0
      ? Boolean(
          userPermissions?.isSuperAdmin ||
            currentOrganizationRole === AppRoles.OrgAdmin
        )
      : true;

  const canViewMembers =
    !!scopeId &&
    hasPermission(AppPermissions.MembersView, scopeTypeName, scopeId);
  const canInviteMembers =
    !!scopeId &&
    hasPermission(AppPermissions.InvitationsCreate, scopeTypeName, scopeId) &&
    canInviteInScope;
  const canViewInvitations =
    !!scopeId &&
    hasPermission(AppPermissions.InvitationsView, scopeTypeName, scopeId);
  const canRevokeInvitations =
    !!scopeId &&
    hasPermission(AppPermissions.InvitationsRevoke, scopeTypeName, scopeId) &&
    canRevokeInScope;
  const canUpdateMemberRole =
    !!scopeId &&
    hasPermission(AppPermissions.MembersUpdateRole, scopeTypeName, scopeId);
  const canRemoveMember =
    !!scopeId &&
    hasPermission(AppPermissions.MembersRemove, scopeTypeName, scopeId);

  const [data, setData] = useState<ScopeMembersResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [actionMsg, setActionMsg] = useState("");

  // Invite modal state
  const [showInvite, setShowInvite] = useState(false);
  const [selectedInvitableUserId, setSelectedInvitableUserId] = useState("");
  const [invRole, setInvRole] = useState("");
  const [inviting, setInviting] = useState(false);
  const [invError, setInvError] = useState("");

  // Role edit state
  const [editingUserId, setEditingUserId] = useState<string | null>(null);
  const [editRole, setEditRole] = useState("");

  const roleOptions =
    scopeType === 0
      ? userPermissions?.isSuperAdmin
        ? organizationRoleOptions
        : organizationRoleOptions.filter((r) => r !== AppRoles.OrgAdmin)
      : currentProjectRole === AppRoles.ProjectManager
      ? projectRoleOptions.filter((r) => r !== AppRoles.ProjectManager)
      : projectRoleOptions;

  const invitableUsers = data?.invitableUsers ?? [];
  const selectedInvitableUser =
    invitableUsers.find((u) => u.userId === selectedInvitableUserId) ?? null;

  const isProtectedOrgAdminTarget =
    scopeType === 0 && !userPermissions?.isSuperAdmin;

  const inviteEligibilitySummary =
    scopeType === 0
      ? "Eligible users are active registered users who are not already members of this organization and do not have a pending invite."
      : "Eligible users are active registered users who belong to this organization, are not already members of this project, and do not have a pending invite.";

  const selectedUserEligibilityLabel = selectedInvitableUser
    ? scopeType === 0
      ? "Eligible: active registered user, not already in this organization, and no pending invite."
      : "Eligible: active organization member, not already in this project, and no pending invite."
    : invitableUsers.length === 0
    ? "No eligible users are available for this scope."
    : "Select a user to see why they are eligible.";

  const fetchData = useCallback(async () => {
    if (!scopeId || !canViewMembers) {
      setLoading(false);
      return;
    }

    setLoading(true);
    setError("");

    try {
      const result = await getMembersByScope(scopeType, scopeId);
      setData(result);
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Failed to load members."));
    } finally {
      setLoading(false);
    }
  }, [canViewMembers, scopeType, scopeId]);

  useEffect(() => {
    if (!scopeId) {
      setScopeDisplayName(scopeLabel);
      return;
    }

    const permissionBasedName =
      scopeType === 0
        ? userPermissions?.organizationRoles.find(
            (r) => r.organizationId === scopeId
          )?.organizationName
        : userPermissions?.projectRoles.find((r) => r.projectId === scopeId)
            ?.projectName;

    if (permissionBasedName) {
      setScopeDisplayName(permissionBasedName);
      return;
    }

    let cancelled = false;

    const loadScopeName = async () => {
      try {
        if (scopeType === 0) {
          const organization = await getOrganizationById(scopeId);
          if (!cancelled) {
            setScopeDisplayName(organization?.name ?? scopeId);
          }
          return;
        }

        const project = await getProjectById(scopeId);
        if (!cancelled) {
          setScopeDisplayName(project.item?.name ?? scopeId);
        }
      } catch {
        if (!cancelled) {
          setScopeDisplayName(scopeId);
        }
      }
    };

    void loadScopeName();

    return () => {
      cancelled = true;
    };
  }, [scopeId, scopeLabel, scopeType, userPermissions]);

  useEffect(() => {
    if (!scopeId) {
      setError("Missing scope id in URL.");
      setLoading(false);
      return;
    }

    void fetchData();
  }, [fetchData, scopeId]);

  const handleInvite = async () => {
    if (!canInviteMembers) {
      setInvError("You do not have permission to invite members.");
      return;
    }

    if (!selectedInvitableUser) {
      setInvError("Please select a registered user to invite.");
      return;
    }

    if (!invRole) {
      setInvError("Please select a role.");
      return;
    }

    setInviting(true);
    setInvError("");
    try {
      await createInvitation({
        scopeType,
        scopeId,
        inviteeEmail: selectedInvitableUser.email,
        role: invRole,
      });
      setShowInvite(false);
      setSelectedInvitableUserId("");
      setInvRole("");
      setActionMsg("Invitation sent!");
      await fetchData();
    } catch (err: unknown) {
      setInvError(getErrorMessage(err, "Failed to send invite."));
    } finally {
      setInviting(false);
    }
  };

  const handleResend = async (invId: string) => {
    if (!canInviteMembers) {
      setError("You do not have permission to resend invitations.");
      return;
    }

    try {
      await resendInvitation(invId);
      setActionMsg("Invitation resent.");
      await fetchData();
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Resend failed."));
    }
  };

  const handleRevoke = async (invId: string) => {
    if (!canRevokeInvitations) {
      setError("You do not have permission to revoke invitations.");
      return;
    }

    if (!window.confirm("Revoke this invitation?")) return;
    try {
      await revokeInvitation(invId);
      setActionMsg("Invitation revoked.");
      await fetchData();
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Revoke failed."));
    }
  };

  const handleRoleUpdate = async (userId: string) => {
    if (!canUpdateMemberRole) {
      setError("You do not have permission to update member roles.");
      return;
    }

    if (!editRole) {
      setError("Please select a valid role.");
      return;
    }

    try {
      await updateMemberRole({ scopeType, scopeId, userId, newRole: editRole });
      setEditingUserId(null);
      setActionMsg("Role updated.");
      await fetchData();
      await refreshPermissions();
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Role update failed."));
    }
  };

  const handleRemoveMember = async (userId: string, name: string) => {
    if (!canRemoveMember) {
      setError("You do not have permission to remove members.");
      return;
    }

    if (!window.confirm(`Remove ${name} from this ${scopeLabel.toLowerCase()}?`))
      return;
    try {
      await removeMember(scopeType, scopeId, userId);
      setActionMsg("Member removed.");
      await fetchData();
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Remove failed."));
    }
  };

  useEffect(() => {
    if (!actionMsg) return;
    const t = setTimeout(() => setActionMsg(""), 3000);
    return () => clearTimeout(t);
  }, [actionMsg]);

  if (!scopeId) {
    return (
      <div className="members-page">
        <div className="members-alert error">Missing scope id in URL.</div>
        <Link to={scopeHomeHref} className="btn-secondary">
          Back to {scopeType === 0 ? "Organizations" : "Projects"}
        </Link>
      </div>
    );
  }

  if (!canViewMembers) {
    return <Navigate to="/forbidden" replace />;
  }

  if (loading) {
    return (
      <div className="members-page">
        <div className="members-loader">Loading members…</div>
      </div>
    );
  }

  return (
    <div className="members-page">
      <header className="members-header">
        <div>
          <h1>Members of {scopeName}</h1>
          <p className="members-subtitle">
            Manage members and pending invitations for this {scopeLabel.toLowerCase()}
          </p>
        </div>
        <div className="members-header-actions">
          <Link to={scopeHomeHref} className="btn-secondary">
            Back to {backLabel}
          </Link>
          <button
            className="btn-primary"
            onClick={() => {
              setSelectedInvitableUserId("");
              setInvRole("");
              setInvError("");
              setShowInvite(true);
            }}
            disabled={!canInviteMembers}
            title={!canInviteMembers ? "You do not have invite permission." : undefined}
          >
            + Invite Member
          </button>
        </div>
      </header>

      {error && <div className="members-alert error">{error}</div>}
      {actionMsg && <div className="members-alert success">{actionMsg}</div>}

      {/* Active members table */}
      <section className="members-section">
        <h2>Members ({data?.members.length ?? 0})</h2>
        <div className="members-table-wrap">
          <table className="members-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Role</th>
                <th>Joined</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {data?.members.map((m) => (
                <tr key={m.userId}>
                  <td className="member-name">
                    {m.firstName} {m.lastName}
                  </td>
                  <td>{m.email}</td>
                  <td>
                    {editingUserId === m.userId ? (
                      <div className="role-edit-row">
                        <select
                          value={editRole}
                          onChange={(e) => setEditRole(e.target.value)}
                        >
                          {roleOptions.map((r) => (
                            <option key={r} value={r}>
                              {r}
                            </option>
                          ))}
                        </select>
                        <button
                          className="btn-sm btn-save"
                          onClick={() => handleRoleUpdate(m.userId)}
                        >
                          Save
                        </button>
                        <button
                          className="btn-sm btn-cancel"
                          onClick={() => setEditingUserId(null)}
                        >
                          ✕
                        </button>
                      </div>
                    ) : (
                      <span className={`role-badge role-${m.role.toLowerCase()}`}>
                        {m.role}
                      </span>
                    )}
                  </td>
                  <td>{new Date(m.joinedAt).toLocaleDateString()}</td>
                  <td className="member-actions">
                    {m.userId !== user?.id && (
                      <>
                          {canUpdateMemberRole &&
                            !(isProtectedOrgAdminTarget && m.role === AppRoles.OrgAdmin) && (
                          <button
                            className="btn-sm btn-edit"
                            onClick={() => {
                              setEditingUserId(m.userId);
                              setEditRole(m.role);
                            }}
                          >
                            Edit Role
                          </button>
                        )}
                          {canRemoveMember &&
                            !(isProtectedOrgAdminTarget && m.role === AppRoles.OrgAdmin) && (
                          <button
                            className="btn-sm btn-danger"
                            onClick={() =>
                              handleRemoveMember(
                                m.userId,
                                `${m.firstName} ${m.lastName}`
                              )
                            }
                          >
                            Remove
                          </button>
                        )}
                          {!canUpdateMemberRole && !canRemoveMember && (
                          <span className="you-label">No actions</span>
                        )}
                          {isProtectedOrgAdminTarget && m.role === AppRoles.OrgAdmin && (
                            <span className="you-label">SuperAdmin only</span>
                          )}
                      </>
                    )}
                    {m.userId === user?.id && (
                      <span className="you-label">You</span>
                    )}
                  </td>
                </tr>
              ))}
              {(!data?.members || data.members.length === 0) && (
                <tr>
                  <td colSpan={5} className="empty-row">
                    No members yet.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>

      {/* Pending invitations */}
      {canViewInvitations ? (
        <section className="members-section">
          <h2>Pending Invitations ({data?.pendingInvitations.length ?? 0})</h2>
          <div className="members-table-wrap">
            <table className="members-table invitations-table">
              <thead>
                <tr>
                  <th>Email</th>
                  <th>Role</th>
                  <th>Invited By</th>
                  <th>Expires</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {data?.pendingInvitations.map((inv) => (
                  <tr key={inv.id}>
                    <td>{inv.inviteeEmail}</td>
                    <td>
                      <span
                        className={`role-badge role-${inv.role.toLowerCase()}`}
                      >
                        {inv.role}
                      </span>
                    </td>
                    <td>{inv.invitedByName || "-"}</td>
                    <td>{new Date(inv.expiresAt).toLocaleDateString()}</td>
                    <td className="member-actions">
                      {canInviteMembers && (
                        <button
                          className="btn-sm btn-edit"
                          onClick={() => handleResend(inv.id)}
                        >
                          Resend
                        </button>
                      )}
                      {canRevokeInvitations && (
                        <button
                          className="btn-sm btn-danger"
                          onClick={() => handleRevoke(inv.id)}
                        >
                          Revoke
                        </button>
                      )}
                      {!canInviteMembers && !canRevokeInvitations && (
                        <span className="you-label">No actions</span>
                      )}
                    </td>
                  </tr>
                ))}
                {(!data?.pendingInvitations ||
                  data.pendingInvitations.length === 0) && (
                  <tr>
                    <td colSpan={5} className="empty-row">
                      No pending invitations.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </section>
      ) : (
        <section className="members-section">
          <h2>Pending Invitations</h2>
          <div className="members-alert error">
            You do not have permission to view invitations for this {scopeLabel.toLowerCase()}.
          </div>
        </section>
      )}

      {/* Invite modal */}
      {showInvite && canInviteMembers && (
        <div className="modal-overlay" onClick={() => setShowInvite(false)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <h3>Invite to {scopeLabel}</h3>
            <div className="form-group">
              <label htmlFor="inv-user">Select registered user</label>
              <p className="form-help">{inviteEligibilitySummary}</p>
              <select
                id="inv-user"
                value={selectedInvitableUserId}
                onChange={(e) => setSelectedInvitableUserId(e.target.value)}
                autoFocus
              >
                <option value="">Choose a user…</option>
                {invitableUsers.map((u) => (
                  <option key={u.userId} value={u.userId}>
                    {u.fullName || `${u.firstName} ${u.lastName}`.trim()} ({u.email})
                  </option>
                ))}
              </select>
              {invitableUsers.length === 0 && (
                <p className="form-help">No eligible registered users available to invite.</p>
              )}
            </div>
            {selectedInvitableUser && (
              <div className="invitable-user-preview">
                <strong>{selectedInvitableUser.fullName || selectedInvitableUser.email}</strong>
                <span>{selectedInvitableUser.email}</span>
                <span>{selectedUserEligibilityLabel}</span>
              </div>
            )}
            {!selectedInvitableUser && (
              <p className="form-help">{selectedUserEligibilityLabel}</p>
            )}
            <div className="form-group">
              <label htmlFor="inv-role">Role</label>
              <select
                id="inv-role"
                value={invRole}
                onChange={(e) => setInvRole(e.target.value)}
              >
                <option value="">Select a role…</option>
                {roleOptions.map((r) => (
                  <option key={r} value={r}>
                    {r}
                  </option>
                ))}
              </select>
            </div>
            {invError && <div className="members-alert error">{invError}</div>}
            <div className="modal-actions">
              <button
                className="btn-secondary"
                onClick={() => setShowInvite(false)}
              >
                Cancel
              </button>
              <button
                className="btn-primary"
                onClick={handleInvite}
                disabled={inviting || !selectedInvitableUserId || !invRole}
              >
                {inviting ? "Sending…" : "Send Invitation"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
