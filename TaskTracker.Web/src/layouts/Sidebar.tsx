import { Button } from "devextreme-react/button";
import { NavLink } from "react-router-dom";
import { useApp } from "../context/AppContext";
import { AppPermissions, type AppPermission } from "../security/permissions";

const navigation = [
  { name: "Dashboard", href: "/dashboard", icon: "home" },
  {
    name: "Projects",
    href: "/projects",
    icon: "folder",
    requiredPermission: AppPermissions.ProjectsView,
  },
  {
    name: "Tasks",
    href: "/tasks",
    icon: "taskcomplete",
    requiredPermission: AppPermissions.TasksView,
  },
  {
    name: "Reports",
    href: "/reports",
    icon: "chart",
    requiredPermission: AppPermissions.TasksView,
  },
  { name: "Settings", href: "/settings", icon: "preferences" },
] as Array<{
  name: string;
  href: string;
  icon: string;
  requiredPermission?: AppPermission;
}>;

export function Sidebar() {
  const { sidebarCollapsed, toggleSidebar, hasPermission } = useApp();

  const visibleNavigation = navigation.filter(
    (item) => !item.requiredPermission || hasPermission(item.requiredPermission)
  );

  return (
    <aside className={`app-sidebar ${sidebarCollapsed ? "is-collapsed" : ""}`}>
      <div className="sidebar-brand">
        <div className="brand-mark">TT</div>
        {!sidebarCollapsed && <span className="brand-name">TaskTracker</span>}
      </div>

      <nav className="sidebar-nav">
        {visibleNavigation.map((item) => (
          <NavLink
            key={item.href}
            to={item.href}
            className={({ isActive }) =>
              `sidebar-link ${isActive ? "is-active" : ""}`
            }
            title={sidebarCollapsed ? item.name : undefined}
          >
            <i className={`dx-icon dx-icon-${item.icon}`} aria-hidden="true" />
            {!sidebarCollapsed && <span>{item.name}</span>}
          </NavLink>
        ))}
      </nav>

      <div className="sidebar-footer">
        <Button
          icon={sidebarCollapsed ? "chevronnext" : "chevronprev"}
          stylingMode="text"
          onClick={toggleSidebar}
          hint={sidebarCollapsed ? "Expand sidebar" : "Collapse sidebar"}
        />
      </div>
    </aside>
  );
}
