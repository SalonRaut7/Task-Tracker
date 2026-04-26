import { useEffect, useState } from "react";
import { Button } from "devextreme-react/button";
import TextBox from "devextreme-react/text-box";
import { Link, Navigate, useLocation, useNavigate } from "react-router-dom";
import { useApp } from "../context/AppContext";
import { getErrorMessage } from "../utils/getErrorMessage";

function isValidEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

function sanitizeReturnUrl(value: string | null): string | null {
  if (!value) return null;
  if (!value.startsWith("/")) return null;
  if (value.startsWith("//")) return null;
  return value;
}

export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { isAuthenticated, bootstrapping, login } = useApp();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");
  const [transitionLoading, setTransitionLoading] = useState(false);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const returnUrlFromQuery = sanitizeReturnUrl(
    new URLSearchParams(location.search).get("returnUrl")
  );
  const returnUrlFromState = sanitizeReturnUrl(
    (location.state as { from?: string } | null)?.from ?? null
  );
  const postLoginPath = returnUrlFromQuery ?? returnUrlFromState ?? "/dashboard";

  useEffect(() => {
    const shouldShowResetSuccess = Boolean(
      (location.state as { passwordResetSuccess?: boolean } | null)?.passwordResetSuccess
    );

    if (!shouldShowResetSuccess) {
      return;
    }

    setSuccessMessage("Password reset successful. Please sign in.");
    setTransitionLoading(true);

    const timer = window.setTimeout(() => {
      setTransitionLoading(false);
    }, 550);

    navigate(location.pathname, { replace: true, state: null });

    return () => window.clearTimeout(timer);
  }, [location.pathname, location.state, navigate]);

  if (bootstrapping) {
    return <div className="page-loader">Loading workspace...</div>;
  }

  if (transitionLoading) {
    return <div className="page-loader">Preparing secure sign-in...</div>;
  }

  if (isAuthenticated) {
    return <Navigate to={postLoginPath} replace />;
  }

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError("");

    if (!isValidEmail(email.trim())) {
      setError("Invalid email.");
      return;
    }

    if (!password.trim()) {
      setError("Password is required.");
      return;
    }

    setLoading(true);

    try {
      await login(email.trim(), password);
      navigate(postLoginPath, { replace: true });
    } catch (requestError) {
      setError(getErrorMessage(requestError, "Unable to sign in right now. Please try again."));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-brand-panel">
        <div className="brand-mark large">TT</div>
        <h1>TaskTracker</h1>
        <p>
          Powerful project management for modern teams. Track tasks,
          collaborate seamlessly, and deliver on time.
        </p>
      </div>

      <div className="auth-form-panel">
        <form className="auth-card" onSubmit={handleSubmit}>
          <h2>Welcome back</h2>
          <p>Sign in to continue</p>

          {error && <div className="form-error">{error}</div>}
          {successMessage && <div className="form-success">{successMessage}</div>}

          <label>
            Email
            <TextBox
              mode="email"
              value={email}
              maxLength={256}
              onValueChanged={(event) => setEmail(String(event.value))}
              placeholder="you@example.com"
              stylingMode="outlined"
            />
          </label>

          <label>
            Password
            <div className="password-field">
              <TextBox
                mode={showPassword ? "text" : "password"}
                value={password}
                maxLength={128}
                onValueChanged={(event) => setPassword(String(event.value))}
                placeholder="Your password"
                stylingMode="outlined"
              />
              <button
                type="button"
                className="password-toggle"
                onClick={() => setShowPassword((previous) => !previous)}
                aria-label={showPassword ? "Hide password" : "Show password"}
              >
                <EyeIcon open={showPassword} />
              </button>
            </div>
          </label>

          <div className="auth-footer-link">
            <Link to="/forgot-password">Forgot password?</Link>
          </div>

          <Button
            text={loading ? "Signing in..." : "Sign in"}
            type="default"
            useSubmitBehavior
            disabled={loading}
          />

          <div className="auth-footer-link">
            No account yet? <Link to="/register">Create one</Link>
          </div>
        </form>
      </div>
    </div>
  );
}

function EyeIcon({ open }: { open: boolean }) {
  if (open) {
    return (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M3 3l18 18" stroke="currentColor" strokeWidth="2" fill="none" strokeLinecap="round" />
        <path
          d="M10.6 10.6A2 2 0 0 0 12 16a2 2 0 0 0 1.4-.6m5.9-1.5A11 11 0 0 0 21 12s-3.5-7-9-7a8.9 8.9 0 0 0-3.2.6M6.1 8.2A11.2 11.2 0 0 0 3 12s3.5 7 9 7a8.6 8.6 0 0 0 3.8-.9"
          stroke="currentColor"
          strokeWidth="2"
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    );
  }

  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path
        d="M1 12s4-7 11-7 11 7 11 7-4 7-11 7S1 12 1 12Z"
        stroke="currentColor"
        strokeWidth="2"
        fill="none"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <circle cx="12" cy="12" r="3" stroke="currentColor" strokeWidth="2" fill="none" />
    </svg>
  );
}
