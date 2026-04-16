import { useMemo, useState } from "react";
import { Button } from "devextreme-react/button";
import TextBox from "devextreme-react/text-box";
import { useNavigate } from "react-router-dom";
import { useApp } from "../context/AppContext";
import { formatDistanceToNow } from "../utils/time";

export function TopNav() {
  const navigate = useNavigate();
  const {
    user,
    notifications,
    theme,
    logout,
    toggleTheme,
    markNotificationRead,
    markAllNotificationsRead,
  } = useApp();
  const [showNotifications, setShowNotifications] = useState(false);
  const [showUserMenu, setShowUserMenu] = useState(false);

  const unreadCount = useMemo(
    () => notifications.filter((item) => !item.read).length,
    [notifications]
  );

  const handleLogout = () => {
    void logout().finally(() => {
      navigate("/login");
    });
  };

  const initials =
    user?.firstName && user?.lastName
      ? `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase()
      : "U";

  return (
    <header className="top-nav">
      <div className="top-nav-search">
        <TextBox
          stylingMode="outlined"
          placeholder="Search tasks, projects..."
          mode="search"
          showClearButton
          width="100%"
        />
      </div>

      <div className="top-nav-actions">
        <Button
          icon={theme === "light" ? "moon" : "sun"}
          stylingMode="text"
          onClick={toggleTheme}
          hint="Toggle theme"
        />

        <div className="dropdown-wrap">
          <Button
            icon="bell"
            stylingMode="text"
            onClick={() => {
              setShowNotifications((prev) => !prev);
              setShowUserMenu(false);
            }}
            hint="Notifications"
          />
          {unreadCount > 0 && <span className="badge-dot">{unreadCount}</span>}

          {showNotifications && (
            <div className="dropdown-panel notifications-panel">
              <div className="dropdown-head">
                <strong>Notifications</strong>
                {unreadCount > 0 && (
                  <button type="button" onClick={markAllNotificationsRead}>
                    Mark all read
                  </button>
                )}
              </div>
              <div className="dropdown-body">
                {notifications.map((item) => (
                  <button
                    key={item.id}
                    type="button"
                    className={`notification-item ${item.read ? "" : "is-unread"}`}
                    onClick={() => markNotificationRead(item.id)}
                  >
                    <div className="notification-title">{item.title}</div>
                    <div className="notification-text">{item.message}</div>
                    <div className="notification-time">
                      {formatDistanceToNow(item.createdAt)}
                    </div>
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>

        <div className="dropdown-wrap">
          <button
            type="button"
            className="user-trigger"
            onClick={() => {
              setShowUserMenu((prev) => !prev);
              setShowNotifications(false);
            }}
          >
            <span className="avatar">{initials}</span>
            <span className="user-name">{user?.fullName ?? "User"}</span>
            <i className="dx-icon dx-icon-chevronnext" aria-hidden="true" />
          </button>

          {showUserMenu && (
            <div className="dropdown-panel user-panel">
              <div className="dropdown-head user-head">
                <strong>{user?.fullName}</strong>
                <small>{user?.email}</small>
              </div>
              <div className="dropdown-body">
                <button type="button" onClick={() => navigate("/settings")}>
                  Profile & settings
                </button>
                <button type="button" className="danger" onClick={handleLogout}>
                  Sign out
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
