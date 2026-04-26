import { useState } from "react";
import { Button } from "devextreme-react/button";
import { Switch } from "devextreme-react/switch";
import TextBox from "devextreme-react/text-box";
import { useApp } from "../context/AppContext";

type SettingsTab = "profile" | "roles" | "notifications" | "appearance";

export function SettingsPage() {
  const { user, userPermissions, theme, toggleTheme } = useApp();
  const [tab, setTab] = useState<SettingsTab>("profile");

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
              <h2>Profile Information</h2>
              <div className="form-grid-two">
                <label>
                  First Name
                  <TextBox value={user?.firstName ?? ""} readOnly />
                </label>
                <label>
                  Last Name
                  <TextBox value={user?.lastName ?? ""} readOnly />
                </label>
              </div>

              <div className="form-grid-two">
                <label>
                  Full Name
                  <TextBox value={user?.fullName ?? ""} readOnly />
                </label>
                <label>
                  Email
                  <TextBox value={user?.email ?? ""} readOnly />
                </label>
              </div>
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
