import { useMemo, useState } from "react";
import { Button } from "devextreme-react/button";
import TextBox from "devextreme-react/text-box";
import { useNavigate } from "react-router-dom";
import { useApp } from "../context/AppContext";
import { formatDistanceToNow } from "../utils/time";

const NOTIFICATION_TYPE_ICON: Record<string, string> = {
  TaskCreated: "add",
  TaskUpdated: "edit",
  TaskDeleted: "trash",
  TaskReassigned: "user",
  TaskDueSoon: "clock",
  TaskOverdue: "warning",
  ProjectUpdated: "edit",
  ProjectDeleted: "trash",
  OrganizationUpdated: "edit",
  OrganizationDeleted: "trash",
};

const NOTIFICATION_TYPE_CLASS: Record<string, string> = {
  TaskCreated: "type-info",
  TaskUpdated: "type-info",
  TaskDeleted: "type-warning",
  TaskReassigned: "type-info",
  TaskDueSoon: "type-warning",
  TaskOverdue: "type-error",
  ProjectUpdated: "type-info",
  ProjectDeleted: "type-warning",
  OrganizationUpdated: "type-info",
  OrganizationDeleted: "type-warning",
};

export function TopNav() {
  const navigate = useNavigate();
  const {
    user,
    notifications,
    toastNotification,
    theme,
    logout,
    toggleTheme,
    markNotificationRead,
    markAllNotificationsRead,
    dismissToast,
  } = useApp();
  const [showNotifications, setShowNotifications] = useState(false);
  const [showUserMenu, setShowUserMenu] = useState(false);

  const unreadCount = useMemo(
    () => notifications.filter((item) => !item.isRead).length,
    [notifications]
  );

  const handleLogout = () => {
    void logout().finally(() => {
      navigate("/login");
    });
  };

  const handleNotificationClick = (notification: (typeof notifications)[0]) => {
    markNotificationRead(notification.id);
    if (notification.taskId && notification.projectId) {
      setShowNotifications(false);
      navigate(`/tasks/${notification.projectId}/${notification.taskId}`);
    }
  };

  const initials =
    user?.firstName && user?.lastName
      ? `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase()
      : "U";

  return (
    <>
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
                  {notifications.length === 0 && (
                    <div className="notification-empty">
                      No notifications yet
                    </div>
                  )}
                  {notifications.map((item) => (
                    <button
                      key={item.id}
                      type="button"
                      className={`notification-item ${item.isRead ? "" : "is-unread"} ${NOTIFICATION_TYPE_CLASS[item.type] ?? ""}`}
                      onClick={() => handleNotificationClick(item)}
                    >
                      <div className="notification-icon">
                        <i
                          className={`dx-icon dx-icon-${NOTIFICATION_TYPE_ICON[item.type] ?? "info"}`}
                          aria-hidden="true"
                        />
                      </div>
                      <div className="notification-content">
                        <div className="notification-text">{item.message}</div>
                        <div className="notification-time">
                          {formatDistanceToNow(item.createdAt)}
                        </div>
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

      {/* Toast notification */}
      {toastNotification && (
        <div className={`toast-notification ${NOTIFICATION_TYPE_CLASS[toastNotification.type] ?? "type-info"}`}>
          <i
            className={`dx-icon dx-icon-${NOTIFICATION_TYPE_ICON[toastNotification.type] ?? "info"}`}
            aria-hidden="true"
          />
          <span className="toast-message">{toastNotification.message}</span>
          <button type="button" className="toast-dismiss" onClick={dismissToast}>
            <i className="dx-icon dx-icon-close" aria-hidden="true" />
          </button>
        </div>
      )}
    </>
  );
}
