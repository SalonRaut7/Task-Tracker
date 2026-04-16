import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useApp } from "../context/AppContext";
import { Sidebar } from "./Sidebar";
import { TopNav } from "./TopNav";

export function MainLayout() {
  const location = useLocation();
  const { isAuthenticated, bootstrapping, sidebarCollapsed } = useApp();

  if (bootstrapping) {
    return <div className="page-loader">Loading workspace...</div>;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  return (
    <div className="app-shell">
      <Sidebar />
      <div
        className={`app-shell-content ${
          sidebarCollapsed ? "sidebar-collapsed" : "sidebar-expanded"
        }`}
      >
        <TopNav />
        <main className="page-container">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
