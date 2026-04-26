import { useState, useEffect } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import { useApp } from "../context/AppContext";
import { acceptInvitation } from "../services/invitationService";
import type { AcceptInvitationResult } from "../types/invitation";
import "../styles/members.css";
import { getErrorMessage } from "../utils/getErrorMessage";

const acceptByToken = new Map<string, Promise<AcceptInvitationResult>>();

function acceptInvitationOnce(token: string): Promise<AcceptInvitationResult> {
  const existing = acceptByToken.get(token);
  if (existing) {
    return existing;
  }

  const promise = acceptInvitation({ token });
  acceptByToken.set(token, promise);

  // Allow retry if the shared request failed.
  promise.catch(() => {
    acceptByToken.delete(token);
  });

  return promise;
}

export default function AcceptInvitePage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const {
    user,
    isAuthenticated,
    bootstrapping,
    refreshPermissions,
    refreshWorkspaceData,
    logout,
  } = useApp();

  const token = searchParams.get("token") ?? "";
  const returnUrl = `${window.location.pathname}${window.location.search}`;
  const [result, setResult] = useState<AcceptInvitationResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [showSwitchAccount, setShowSwitchAccount] = useState(false);
  const [processed, setProcessed] = useState(false);

  const scopeMembersPath =
    result?.success && result.scopeId
      ? result.scopeType === 0
        ? `/organizations/${result.scopeId}/members`
        : result.scopeType === 1
        ? `/projects/${result.scopeId}/members`
        : null
      : null;

  useEffect(() => {
    if (bootstrapping || processed) return;

    if (!isAuthenticated) {
      // Redirect to login with return URL
      navigate(`/login?returnUrl=${encodeURIComponent(returnUrl)}`, {
        replace: true,
      });
      return;
    }

    if (!token) {
      setError("Missing invitation token.");
      setProcessed(true);
      return;
    }

    const doAccept = async () => {
      setLoading(true);
      setError("");
      try {
        const res = await acceptInvitationOnce(token);
        setResult(res);
        if (res.success) {
          await refreshPermissions();
          await refreshWorkspaceData();
        }
      } catch (err: unknown) {
        const message = getErrorMessage(err, "Failed to accept invitation.");
        setError(message);
        setShowSwitchAccount(
          message.toLowerCase().includes("different email address")
        );
      } finally {
        setLoading(false);
        setProcessed(true);
      }
    };

    void doAccept();
  }, [
    bootstrapping,
    isAuthenticated,
    token,
    processed,
    navigate,
    refreshPermissions,
    refreshWorkspaceData,
    returnUrl,
  ]);

  const handleSwitchAccount = async () => {
    await logout();
    navigate(`/login?returnUrl=${encodeURIComponent(returnUrl)}`, {
      replace: true,
    });
  };

  if (bootstrapping || loading) {
    return (
      <div className="accept-invite-page">
        <div className="accept-card">
          <div className="accept-loader">Accepting invitation…</div>
        </div>
      </div>
    );
  }

  return (
    <div className="accept-invite-page">
      <div className="accept-card">
        <h1>Invitation</h1>
        {error && (
          <>
            <div className="members-alert error">{error}</div>
            {showSwitchAccount && (
              <div className="accept-details">
                {user?.email ? (
                  <p>
                    Signed in as <strong>{user.email}</strong>. Switch accounts to
                    accept this invitation.
                  </p>
                ) : null}
                <div className="modal-actions" style={{ justifyContent: "center" }}>
                  <button className="btn-primary" onClick={handleSwitchAccount}>
                    Switch Account and Continue
                  </button>
                </div>
              </div>
            )}
          </>
        )}
        {result && (
          <>
            <div className={`members-alert ${result.success ? "success" : "error"}`}>
              {result.message}
            </div>
            {result.success && (
              <div className="accept-details">
                <p>
                  You've been added as <strong>{result.role}</strong>.
                </p>
                <div className="modal-actions" style={{ justifyContent: "center" }}>
                  {scopeMembersPath && (
                    <button
                      className="btn-secondary"
                      onClick={() => navigate(scopeMembersPath)}
                    >
                      Go to Team
                    </button>
                  )}
                  <button
                    className="btn-primary"
                    onClick={() => navigate("/dashboard")}
                  >
                    Go to Dashboard
                  </button>
                </div>
              </div>
            )}
          </>
        )}
        {!result && !error && !loading && (
          <p>Processing your invitation…</p>
        )}
      </div>
    </div>
  );
}
