import { useEffect, useState } from "react";
import { Button } from "devextreme-react/button";
import { Switch } from "devextreme-react/switch";
import TextBox from "devextreme-react/text-box";
import { useApp } from "../context/AppContext";
import { getErrorMessage } from "../utils/getErrorMessage";
import type { CurrentUserProfile } from "../types/app";
import { getCurrentUserProfile } from "../services/userService";

type SettingsTab = "profile" | "roles" | "notifications" | "appearance";

function isValidPersonName(value: string): boolean {
  return /^[A-Za-z][A-Za-z\s'-]*$/.test(value);
}

export function SettingsPage() {
  const { user, userPermissions, theme, toggleTheme, updateCurrentUserProfile } = useApp();
  const [tab, setTab] = useState<SettingsTab>("profile");
  const [profile, setProfile] = useState<CurrentUserProfile | null>(null);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [profileLoading, setProfileLoading] = useState(false);
  const [profileSaving, setProfileSaving] = useState(false);
  const [profileError, setProfileError] = useState("");
  const [profileInfo, setProfileInfo] = useState("");
  const [isEditingProfile, setIsEditingProfile] = useState(false);

  useEffect(() => {
    if (tab !== "profile") {
      return;
    }

    let active = true;

    const loadProfile = async () => {
      setProfileLoading(true);
      setProfileError("");

      try {
        const data = await getCurrentUserProfile();
        if (!active) {
          return;
        }

        setProfile(data);
        setFirstName(data.firstName);
        setLastName(data.lastName);
        setIsEditingProfile(false);
      } catch (error) {
        if (!active) {
          return;
        }

        setProfileError(getErrorMessage(error, "Unable to load your profile."));
      } finally {
        if (active) {
          setProfileLoading(false);
        }
      }
    };

    void loadProfile();

    return () => {
      active = false;
    };
  }, [tab, user?.id]);

  const handleEditProfile = () => {
    if (!profile) {
      return;
    }

    setProfileError("");
    setProfileInfo("");
    setFirstName(profile.firstName);
    setLastName(profile.lastName);
    setIsEditingProfile(true);
  };

  const handleCancelEdit = () => {
    if (profile) {
      setFirstName(profile.firstName);
      setLastName(profile.lastName);
    }

    setProfileError("");
    setProfileInfo("");
    setIsEditingProfile(false);
  };

  const handleSaveProfile = async () => {
    setProfileError("");
    setProfileInfo("");

    if (!firstName.trim()) {
      setProfileError("First name is required.");
      return;
    }

    if (firstName.trim().length > 100) {
      setProfileError("First name must be 100 characters or less.");
      return;
    }

    if (!isValidPersonName(firstName.trim())) {
      setProfileError("First name can contain letters, spaces, apostrophes, and hyphens only.");
      return;
    }

    if (!lastName.trim()) {
      setProfileError("Last name is required.");
      return;
    }

    if (lastName.trim().length > 100) {
      setProfileError("Last name must be 100 characters or less.");
      return;
    }

    if (!isValidPersonName(lastName.trim())) {
      setProfileError("Last name can contain letters, spaces, apostrophes, and hyphens only.");
      return;
    }

    setProfileSaving(true);

    try {
      const updated = await updateCurrentUserProfile(firstName, lastName);
      setProfile(updated);
      setFirstName(updated.firstName);
      setLastName(updated.lastName);
      setIsEditingProfile(false);
      setProfileInfo("Profile updated successfully.");
    } catch (error) {
      setProfileError(getErrorMessage(error, "Unable to update your profile."));
    } finally {
      setProfileSaving(false);
    }
  };

  return (
    <div className="page-stack">
      <section>
        <h1>Settings</h1>
        <p className="page-subtitle">Manage your account and app preferences</p>
      </section>

      <section className="settings-layout">
        <nav className="settings-tabs card">
          <Button
            text="Profile"
            stylingMode={tab === "profile" ? "contained" : "text"}
            onClick={() => setTab("profile")}
          />
          <Button
            text="Roles & Permissions"
            stylingMode={tab === "roles" ? "contained" : "text"}
            onClick={() => setTab("roles")}
          />
          <Button
            text="Notifications"
            stylingMode={tab === "notifications" ? "contained" : "text"}
            onClick={() => setTab("notifications")}
          />
          <Button
            text="Appearance"
            stylingMode={tab === "appearance" ? "contained" : "text"}
            onClick={() => setTab("appearance")}
          />
        </nav>

        <div className="settings-content">
          {tab === "profile" && (
            <section className="card">
              <div className="section-header" style={{ alignItems: "center" }}>
                <div>
                  <h2>Profile Information</h2>
                  <p className="page-subtitle">Manage the personal fields used across the app.</p>
                </div>
                <div className="inline-actions">
                  {!isEditingProfile ? (
                    <Button
                      text="Edit profile"
                      onClick={handleEditProfile}
                      disabled={Boolean(userPermissions?.isSuperAdmin)}
                    />
                  ) : (
                    <>
                      <Button text={profileSaving ? "Saving..." : "Save changes"} type="default" onClick={handleSaveProfile} disabled={profileSaving} />
                      <Button text="Cancel" stylingMode="text" onClick={handleCancelEdit} disabled={profileSaving} />
                    </>
                  )}
                </div>
              </div>

              {userPermissions?.isSuperAdmin && (
                <p className="page-subtitle">SuperAdmin accounts cannot edit their own profile.</p>
              )}

              {profileError && <div className="form-error">{profileError}</div>}
              {profileInfo && <div className="form-success">{profileInfo}</div>}

              {profileLoading && <p className="page-subtitle">Loading profile...</p>}

              {profile && !profileLoading && (
                <div className="form-grid-two">
                  <label>
                    First Name
                    <TextBox
                      value={firstName}
                      readOnly={!isEditingProfile}
                      maxLength={100}
                      stylingMode="outlined"
                      onValueChanged={(event) => setFirstName(String(event.value ?? ""))}
                    />
                  </label>
                  <label>
                    Last Name
                    <TextBox
                      value={lastName}
                      readOnly={!isEditingProfile}
                      maxLength={100}
                      stylingMode="outlined"
                      onValueChanged={(event) => setLastName(String(event.value ?? ""))}
                    />
                  </label>

                  <label>
                    Full Name
                    <TextBox value={`${firstName.trim()} ${lastName.trim()}`.trim()} readOnly stylingMode="outlined" />
                  </label>
                  <label>
                    Email
                    <TextBox value={profile.email} readOnly stylingMode="outlined" />
                  </label>
                </div>
              )}

              {!profileLoading && !profile && (
                <p className="page-subtitle">Unable to load profile data.</p>
              )}
            </section>
          )}

          {tab === "roles" && (
            <section className="card">
              <h2>Your Access</h2>
              {userPermissions?.isSuperAdmin && (
                <div style={{ marginBottom: "0.75rem" }}>
                  <strong>Global</strong>
                  <div className="chip-row" style={{ marginTop: "0.5rem" }}>
                    <span className="role-chip">SuperAdmin</span>
                  </div>
                </div>
              )}

              {!!userPermissions?.organizationRoles.length && (
                <div style={{ marginBottom: "0.75rem" }}>
                  <strong>Organization Roles</strong>
                  <div className="chip-row" style={{ marginTop: "0.5rem" }}>
                    {userPermissions.organizationRoles.map((orgRole) => (
                      <span
                        key={`${orgRole.organizationId}-${orgRole.role}`}
                        className="role-chip"
                      >
                        {orgRole.organizationName}: {orgRole.role}
                      </span>
                    ))}
                  </div>
                </div>
              )}

              {!!userPermissions?.projectRoles.length && (
                <div>
                  <strong>Project Roles</strong>
                  <div className="chip-row" style={{ marginTop: "0.5rem" }}>
                    {userPermissions.projectRoles.map((projectRole) => (
                      <span
                        key={`${projectRole.projectId}-${projectRole.role}`}
                        className="role-chip"
                      >
                        {projectRole.projectName}: {projectRole.role}
                      </span>
                    ))}
                  </div>
                </div>
              )}

              {!userPermissions?.isSuperAdmin &&
                !userPermissions?.organizationRoles.length &&
                !userPermissions?.projectRoles.length &&
                !user?.roles.length && (
                  <p className="page-subtitle">
                    No roles are currently assigned to your account.
                  </p>
                )}

              {!userPermissions && (
                <p className="page-subtitle">
                  Loading scoped roles and permissions...
                </p>
              )}
            </section>
          )}

          {tab === "notifications" && (
            <section className="card toggle-list">
              <h2>Notification Preferences</h2>
              <PreferenceToggle label="Due date reminders" />
              <PreferenceToggle label="Task completion updates" />
              <PreferenceToggle label="Overdue task alerts" />
              <PreferenceToggle label="Email notifications" defaultValue={false} />
            </section>
          )}

          {tab === "appearance" && (
            <section className="card toggle-list">
              <h2>Appearance</h2>
              <div className="toggle-item">
                <div>
                  <strong>Theme</strong>
                  <p>{theme === "dark" ? "Dark mode" : "Light mode"}</p>
                </div>
                <Switch value={theme === "dark"} onValueChanged={toggleTheme} />
              </div>
            </section>
          )}
        </div>
      </section>
    </div>
  );
}

function PreferenceToggle({
  label,
  defaultValue = true,
}: {
  label: string;
  defaultValue?: boolean;
}) {
  const [value, setValue] = useState(defaultValue);

  return (
    <div className="toggle-item">
      <div>
        <strong>{label}</strong>
      </div>
      <Switch
        value={value}
        onValueChanged={(event) => setValue(Boolean(event.value))}
      />
    </div>
  );
}
