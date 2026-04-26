import { useEffect, useState } from "react";
import { Button } from "devextreme-react/button";
import TextBox from "devextreme-react/text-box";
import { Link, Navigate, useNavigate } from "react-router-dom";
import { useApp } from "../context/AppContext";
import { getErrorMessage } from "../utils/getErrorMessage";

function isValidEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

function isValidPersonName(value: string): boolean {
  return /^[A-Za-z][A-Za-z\s'-]*$/.test(value);
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

export function RegisterPage() {
  const RESEND_COOLDOWN_SECONDS = 30;
  const navigate = useNavigate();
  const { isAuthenticated, bootstrapping, register, verifyEmail, resendOtp, login } = useApp();
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [otpCode, setOtpCode] = useState("");
  const [step, setStep] = useState<"register" | "verify">("register");
  const [info, setInfo] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [resendLoading, setResendLoading] = useState(false);
  const [resendCooldownSeconds, setResendCooldownSeconds] = useState(0);

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

  const handleRegisterSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError("");
    setInfo("");

    if (!firstName.trim()) {
      setError("First name is required.");
      return;
    }

    if (firstName.trim().length > 100) {
      setError("First name must be 100 characters or less.");
      return;
    }

    if (!isValidPersonName(firstName.trim())) {
      setError("First name can contain letters, spaces, apostrophes, and hyphens only.");
      return;
    }

    if (!lastName.trim()) {
      setError("Last name is required.");
      return;
    }

    if (lastName.trim().length > 100) {
      setError("Last name must be 100 characters or less.");
      return;
    }

    if (!isValidPersonName(lastName.trim())) {
      setError("Last name can contain letters, spaces, apostrophes, and hyphens only.");
      return;
    }

    if (!isValidEmail(email.trim())) {
      setError("Invalid email.");
      return;
    }

    const passwordError = validatePassword(password);
    if (passwordError) {
      setError(passwordError);
      return;
    }

    if (password !== confirmPassword) {
      setError("Passwords do not match.");
      return;
    }

    setLoading(true);

    try {
      const response = await register({
        email: email.trim(),
        password,
        firstName: firstName.trim(),
        lastName: lastName.trim(),
      });

      setInfo(response.message);
      setStep("verify");
      setResendCooldownSeconds(RESEND_COOLDOWN_SECONDS);
    } catch (requestError) {
      setError(getErrorMessage(requestError, "Registration failed. Please try again."));
    } finally {
      setLoading(false);
    }
  };

  const handleVerifySubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError("");
    setInfo("");

    if (!/^\d{6}$/.test(otpCode.trim())) {
      setError("Verification code must be exactly 6 digits.");
      return;
    }

    setLoading(true);
    try {
      const trimmedEmail = email.trim();
      const response = await verifyEmail(trimmedEmail, otpCode.trim());
      setInfo(response.message);
      await login(trimmedEmail, password);
      navigate("/dashboard", { replace: true });
    } catch (requestError) {
      setError(getErrorMessage(requestError, "Verification failed. Please try again."));
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
      const message = await resendOtp(email);
      setInfo(message);
      setResendCooldownSeconds(RESEND_COOLDOWN_SECONDS);
    } catch (requestError) {
      setError(getErrorMessage(requestError, "Unable to resend OTP right now."));
    } finally {
      setResendLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-brand-panel">
        <div className="brand-mark large">TT</div>
        <h1>{step === "register" ? "Create account" : "Verify your email"}</h1>
        <p>
          {step === "register"
            ? "Start your workspace and secure it with strong credentials."
            : "Enter the 6-digit code sent to your inbox to activate your account."}
        </p>
      </div>

      <div className="auth-form-panel">
        <form
          className="auth-card"
          onSubmit={step === "register" ? handleRegisterSubmit : handleVerifySubmit}
        >
          <h2>{step === "register" ? "Join TaskTracker" : "Verify your email"}</h2>
          <p>
            {step === "register"
              ? "Build, track and deliver faster"
              : `Code sent to ${email}`}
          </p>

          {error && <div className="form-error">{error}</div>}
          {info && <div className="form-success">{info}</div>}

          {step === "register" && (
            <>
              <div className="form-grid-two">
                <label>
                  First name
                  <TextBox
                    value={firstName}
                    maxLength={100}
                    onValueChanged={(event) => setFirstName(String(event.value))}
                    placeholder="Jane"
                    stylingMode="outlined"
                  />
                </label>

                <label>
                  Last name
                  <TextBox
                    value={lastName}
                    maxLength={100}
                    onValueChanged={(event) => setLastName(String(event.value))}
                    placeholder="Doe"
                    stylingMode="outlined"
                  />
                </label>
              </div>

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
                    placeholder="8+ chars, upper/lower/number/symbol"
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

              <label>
                Confirm password
                <div className="password-field">
                  <TextBox
                    mode={showConfirmPassword ? "text" : "password"}
                    value={confirmPassword}
                    maxLength={128}
                    onValueChanged={(event) => setConfirmPassword(String(event.value))}
                    placeholder="Repeat password"
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

          {step === "verify" && (
            <label>
              Verification code
              <TextBox
                value={otpCode}
                maxLength={6}
                onValueChanged={(event) => setOtpCode(String(event.value ?? ""))}
                placeholder="6-digit code"
                stylingMode="outlined"
              />
            </label>
          )}

          <Button
            text={
              loading
                ? step === "register"
                  ? "Creating account..."
                  : "Verifying..."
                : step === "register"
                ? "Create account"
                : "Verify and sign in"
            }
            type="default"
            useSubmitBehavior
            disabled={loading}
          />

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
              disabled={resendLoading || loading || resendCooldownSeconds > 0}
              onClick={handleResendOtp}
            />
          )}

          <div className="auth-footer-link">
            Already registered? <Link to="/login">Sign in</Link>
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
