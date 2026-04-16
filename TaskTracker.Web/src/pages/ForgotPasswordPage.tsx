import { useEffect, useState } from "react";
import { Button } from "devextreme-react/button";
import TextBox from "devextreme-react/text-box";
import { Link, Navigate, useNavigate } from "react-router-dom";
import {
  forgotPassword,
  resetPassword,
  verifyPasswordResetOtp,
} from "../services/authService";
import { useApp } from "../context/AppContext";
import { ApiError } from "../services/apiClient";

function isValidEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

function validatePassword(password: string): string | null {
  if (password.length < 8) {
    return "Password must be at least 8 characters.";
  }
  if (!/[A-Z]/.test(password)) {
    return "Password must include at least one uppercase letter.";
  }
  if (!/[a-z]/.test(password)) {
    return "Password must include at least one lowercase letter.";
  }
  if (!/[0-9]/.test(password)) {
    return "Password must include at least one number.";
  }
  if (!/[^a-zA-Z0-9]/.test(password)) {
    return "Password must include at least one symbol.";
  }

  return null;
}

export function ForgotPasswordPage() {
  const RESEND_COOLDOWN_SECONDS = 30;
  const navigate = useNavigate();
  const { isAuthenticated, bootstrapping } = useApp();
  const [step, setStep] = useState<"request" | "verify" | "reset">("request");
  const [email, setEmail] = useState("");
  const [otpCode, setOtpCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [resendLoading, setResendLoading] = useState(false);
  const [resendCooldownSeconds, setResendCooldownSeconds] = useState(0);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [error, setError] = useState("");
  const [info, setInfo] = useState("");

  useEffect(() => {
    if (resendCooldownSeconds <= 0) {
      return;
    }

    const timer = window.setInterval(() => {
      setResendCooldownSeconds((previous) => (previous > 0 ? previous - 1 : 0));
    }, 1000);

    return () => window.clearInterval(timer);
  }, [resendCooldownSeconds]);

  if (bootstrapping) {
    return <div className="page-loader">Loading workspace...</div>;
  }

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  const handleRequestReset = async (event: React.FormEvent) => {
    event.preventDefault();
    setError("");
    setInfo("");

    if (!isValidEmail(email.trim())) {
      setError("Invalid email.");
      return;
    }

    setLoading(true);

    try {
      const response = await forgotPassword({ email: email.trim() });
      setInfo(response.message);
      setStep("verify");
      setResendCooldownSeconds(RESEND_COOLDOWN_SECONDS);
    } catch (requestError) {
      if (requestError instanceof ApiError) {
        setError(requestError.message);
      } else if (requestError instanceof Error) {
        setError(requestError.message);
      } else {
        setError("Unable to process your request right now.");
      }
    } finally {
      setLoading(false);
    }
  };

  const handleVerifyOtp = async (event: React.FormEvent) => {
    event.preventDefault();
    setError("");
    setInfo("");

    if (!/^\d{6}$/.test(otpCode.trim())) {
      setError("Reset code must be exactly 6 digits.");
      return;
    }

    setLoading(true);

    try {
      const response = await verifyPasswordResetOtp({
        email: email.trim(),
        otpCode: otpCode.trim(),
      });
      setInfo(response.message);
      setStep("reset");
    } catch (requestError) {
      if (requestError instanceof ApiError) {
        setError(requestError.message);
      } else if (requestError instanceof Error) {
        setError(requestError.message);
      } else {
        setError("Unable to verify reset code right now.");
      }
    } finally {
      setLoading(false);
    }
  };

  const handleResetPassword = async (event: React.FormEvent) => {
    event.preventDefault();
    setError("");
    setInfo("");

    if (!isValidEmail(email.trim())) {
      setError("Invalid email.");
      return;
    }

    if (!/^\d{6}$/.test(otpCode.trim())) {
      setError("Reset code must be exactly 6 digits.");
      return;
    }

    const passwordError = validatePassword(newPassword);
    if (passwordError) {
      setError(passwordError);
      return;
    }

    if (newPassword !== confirmPassword) {
      setError("Passwords do not match.");
      return;
    }

    setLoading(true);

    try {
      const response = await resetPassword({
        email: email.trim(),
        otpCode: otpCode.trim(),
        newPassword,
      });
      setInfo(response.message);
      navigate("/login", { replace: true, state: { passwordResetSuccess: true } });
    } catch (requestError) {
      if (requestError instanceof ApiError) {
        setError(requestError.message);
      } else if (requestError instanceof Error) {
        setError(requestError.message);
      } else {
        setError("Unable to reset password right now.");
      }
    } finally {
      setLoading(false);
    }
  };

  const handleResendOtp = async () => {
    if (resendCooldownSeconds > 0) {
      return;
    }

    setError("");
    setInfo("");
    setResendLoading(true);

    try {
      const response = await forgotPassword({ email: email.trim() });
      setInfo(response.message);
      setResendCooldownSeconds(RESEND_COOLDOWN_SECONDS);
    } catch (requestError) {
      if (requestError instanceof ApiError) {
        setError(requestError.message);
      } else if (requestError instanceof Error) {
        setError(requestError.message);
      } else {
        setError("Unable to resend reset code right now.");
      }
    } finally {
      setResendLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-brand-panel">
        <div className="brand-mark large">TT</div>
        <h1>{step === "request" ? "Forgot password" : "Reset your password"}</h1>
        <p>
          {step === "request"
            ? "Request a one-time code to securely reset your password."
            : step === "verify"
            ? "Verify your reset code before changing your password."
            : "Choose a strong new password for your account."}
        </p>
      </div>

      <div className="auth-form-panel">
        <form
          className="auth-card"
          onSubmit={
            step === "request"
              ? handleRequestReset
              : step === "verify"
              ? handleVerifyOtp
              : handleResetPassword
          }
        >
          <h2>
            {step === "request"
              ? "Recover access"
              : step === "verify"
              ? "Verify reset code"
              : "Create a new password"}
          </h2>

          {error && <div className="form-error">{error}</div>}
          {info && <div className="form-success">{info}</div>}

          <label>
            Email
            <TextBox
              mode="email"
              value={email}
              onValueChanged={(event) => setEmail(String(event.value))}
              placeholder="you@example.com"
              stylingMode="outlined"
            />
          </label>

          {step !== "request" && (
            <>
              <label>
                Reset code
                <TextBox
                  value={otpCode}
                  onValueChanged={(event) => setOtpCode(String(event.value ?? ""))}
                  placeholder="6-digit code"
                  stylingMode="outlined"
                />
              </label>

              {step === "verify" && (
                <Button
                  text={
                    resendLoading
                      ? "Resending..."
                      : resendCooldownSeconds > 0
                      ? `Resend code in ${resendCooldownSeconds}s`
                      : "Resend code"
                  }
                  stylingMode="outlined"
                  disabled={loading || resendLoading || resendCooldownSeconds > 0}
                  onClick={handleResendOtp}
                />
              )}
            </>
          )}

          {step === "reset" && (
            <>

              <label>
                New password
                <div className="password-field">
                  <TextBox
                    mode={showNewPassword ? "text" : "password"}
                    value={newPassword}
                    onValueChanged={(event) => setNewPassword(String(event.value ?? ""))}
                    placeholder="8+ chars, upper/lower/number/symbol"
                    stylingMode="outlined"
                  />
                  <button
                    type="button"
                    className="password-toggle"
                    onClick={() => setShowNewPassword((previous) => !previous)}
                    aria-label={showNewPassword ? "Hide password" : "Show password"}
                  >
                    <EyeIcon open={showNewPassword} />
                  </button>
                </div>
              </label>

              <label>
                Confirm password
                <div className="password-field">
                  <TextBox
                    mode={showConfirmPassword ? "text" : "password"}
                    value={confirmPassword}
                    onValueChanged={(event) => setConfirmPassword(String(event.value ?? ""))}
                    placeholder="Repeat new password"
                    stylingMode="outlined"
                  />
                  <button
                    type="button"
                    className="password-toggle"
                    onClick={() => setShowConfirmPassword((previous) => !previous)}
                    aria-label={showConfirmPassword ? "Hide password" : "Show password"}
                  >
                    <EyeIcon open={showConfirmPassword} />
                  </button>
                </div>
              </label>
            </>
          )}

          <Button
            text={
              loading
                ? step === "request"
                  ? "Sending code..."
                  : step === "verify"
                  ? "Verifying code..."
                  : "Resetting password..."
                : step === "request"
                ? "Send reset code"
                : step === "verify"
                ? "Verify code"
                : "Reset password"
            }
            type="default"
            useSubmitBehavior
            disabled={loading}
          />

          <div className="auth-footer-link">
            Back to <Link to="/login">Sign in</Link>
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
