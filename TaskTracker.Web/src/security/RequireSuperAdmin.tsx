import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useApp } from "../context/AppContext";

type RequireSuperAdminProps = {
  children: ReactNode;
};

export function RequireSuperAdmin({ children }: RequireSuperAdminProps) {
  const location = useLocation();
  const { isAuthenticated, bootstrapping, permissionsLoaded, userPermissions } = useApp();

  if (bootstrapping || (isAuthenticated && !permissionsLoaded)) {
    return <div className="page-loader">Loading workspace...</div>;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  if (!userPermissions?.isSuperAdmin) {
    return <Navigate to="/forbidden" replace state={{ from: location.pathname }} />;
  }

  return <>{children}</>;
}
